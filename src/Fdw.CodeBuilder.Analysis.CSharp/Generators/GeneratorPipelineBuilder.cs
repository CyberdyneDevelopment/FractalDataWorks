using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using Fdw.CodeBuilder.Analysis.CSharp.Compilations;
using Fdw.CodeBuilder.Analysis.CSharp.Helpers;
using Microsoft.CodeAnalysis;

namespace Fdw.CodeBuilder.Analysis.CSharp.Generators;

/// <summary>
/// Fluent API for building complex generator test scenarios.
/// Combines functionality of other test utilities for readable test setup.
/// </summary>
/// <remarks>
/// This code is excluded from code coverage because it is test infrastructure code that supports testing but is not production code.
/// </remarks>
[ExcludeFromCodeCoverage]
public class GeneratorPipelineBuilder
{
    private readonly AssemblyCompilationBuilder _compilationBuilder;
    private readonly List<IIncrementalGenerator> _generators;
    private readonly Dictionary<string, object> _services;
    private CancellationToken _cancellationToken;
    private bool _verifyNoErrors;
    private readonly List<Action<GeneratorPipelineResult>> _assertions;

    private GeneratorPipelineBuilder()
    {
        _compilationBuilder = new AssemblyCompilationBuilder();
        _generators = [];
        _services = new Dictionary<string, object>(StringComparer.Ordinal);
        _cancellationToken = CancellationToken.None;
        _verifyNoErrors = true;
        _assertions = [];
    }

    /// <summary>
    /// Creates a new pipeline builder.
    /// </summary>
    public static GeneratorPipelineBuilder Create()
    {
        return new GeneratorPipelineBuilder();
    }

    /// <summary>
    /// Adds an assembly-level attribute by type.
    /// </summary>
    public GeneratorPipelineBuilder WithAssemblyAttribute<TAttribute>() where TAttribute : Attribute
    {
        _compilationBuilder.WithAssemblyAttribute<TAttribute>();
        return this;
    }

    /// <summary>
    /// Adds an assembly-level attribute from source.
    /// </summary>
    public GeneratorPipelineBuilder WithAssemblyAttribute(string attributeSource)
    {
        _compilationBuilder.WithAssemblyAttribute(attributeSource);
        return this;
    }

    /// <summary>
    /// Adds a generator by type (must have parameterless constructor).
    /// </summary>
    public GeneratorPipelineBuilder AddGenerator<TGenerator>()
        where TGenerator : IIncrementalGenerator, new()
    {
        _generators.Add(new TGenerator());
        return this;
    }

    /// <summary>
    /// Adds a generator instance.
    /// </summary>
    public GeneratorPipelineBuilder AddGenerator(IIncrementalGenerator generator)
    {
        if (generator == null)
            throw new ArgumentNullException(nameof(generator));

        _generators.Add(generator);
        return this;
    }

    /// <summary>
    /// Adds source code to the compilation.
    /// </summary>
    public GeneratorPipelineBuilder WithSource(string source)
    {
        _compilationBuilder.WithSource(source);
        return this;
    }

    /// <summary>
    /// Adds a reference to another compilation.
    /// </summary>
    public GeneratorPipelineBuilder WithReference(Compilation compilation)
    {
        _compilationBuilder.WithReference(compilation);
        return this;
    }

    /// <summary>
    /// Adds a reference to an assembly.
    /// </summary>
    public GeneratorPipelineBuilder WithReference(Assembly assembly)
    {
        _compilationBuilder.WithReference(assembly);
        return this;
    }

    /// <summary>
    /// Sets the assembly name.
    /// </summary>
    public GeneratorPipelineBuilder WithAssemblyName(string name)
    {
        _compilationBuilder.WithAssemblyName(name);
        return this;
    }

    /// <summary>
    /// Registers a service for testing (will be available via ServiceRegistrationTestHelper).
    /// </summary>
    public GeneratorPipelineBuilder WithService<TService>(TService service) where TService : class
    {
        _services[typeof(TService).FullName] = service;
        return this;
    }

    /// <summary>
    /// Sets a cancellation token for the generator run.
    /// </summary>
    public GeneratorPipelineBuilder WithCancellationToken(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        return this;
    }

    /// <summary>
    /// Disables automatic error verification.
    /// </summary>
    public GeneratorPipelineBuilder AllowErrors()
    {
        _verifyNoErrors = false;
        return this;
    }

    /// <summary>
    /// Adds an assertion to run after the pipeline completes.
    /// </summary>
    public GeneratorPipelineBuilder Assert(Action<GeneratorPipelineResult> assertion)
    {
        if (assertion == null)
            throw new ArgumentNullException(nameof(assertion));

        _assertions.Add(assertion);
        return this;
    }

    /// <summary>
    /// Adds an assertion that a specific source file was generated.
    /// </summary>
    public GeneratorPipelineBuilder AssertGeneratedSource(string hintName, Action<string>? sourceAssertion = null)
    {
        _assertions.Add(result =>
        {
            var source = result.GeneratorResult.GetAllGeneratedSources()
                .FirstOrDefault(s => string.Equals(s.HintName, hintName, StringComparison.Ordinal));

            if (source.HintName == null)
            {
                var available = string.Join(", ",
                    result.GeneratorResult.GetAllGeneratedSources().Select(s => s.HintName));
                throw new InvalidOperationException(
                    $"Expected generated source with hint name '{hintName}'. Available: {available}");
            }

            sourceAssertion?.Invoke(source.SourceText.ToString());
        });

        return this;
    }

    /// <summary>
    /// Adds an assertion about the number of generated sources.
    /// </summary>
    public GeneratorPipelineBuilder AssertGeneratedSourceCount(int expectedCount)
    {
        _assertions.Add(result =>
        {
            var actualCount = result.GeneratorResult.GetAllGeneratedSources().Count();
            if (actualCount != expectedCount)
            {
                throw new InvalidOperationException(
                    $"Expected {expectedCount} generated sources but found {actualCount}");
            }
        });

        return this;
    }

    /// <summary>
    /// Builds and runs the generator pipeline.
    /// </summary>
    public GeneratorPipelineResult Build()
    {
        var compilation = BuildCompilation();
        var generatorResult = RunGeneratorsInternal(compilation);
        var result = CreateResult(compilation, generatorResult);
        RunAssertions(result);
        return result;
    }

    private Compilation BuildCompilation()
    {
        return _verifyNoErrors
            ? _compilationBuilder.BuildAndVerify()
            : _compilationBuilder.Build();
    }

    private void RegisterServices(Compilation compilation)
    {
        foreach (var kvp in _services)
        {
            var serviceType = Type.GetType(kvp.Key);
            if (serviceType != null)
            {
                var registerMethod = typeof(ServiceRegistrationTestHelper)
                    .GetMethod(nameof(ServiceRegistrationTestHelper.Register))
                    ?.MakeGenericMethod(serviceType);

                registerMethod?.Invoke(null, [compilation, kvp.Value]);
            }
        }
    }

    private MultiGeneratorTestHelper.MultiGeneratorResult RunGeneratorsInternal(Compilation compilation)
    {
        RegisterServices(compilation);

        if (_generators.Count > 0)
        {
            return _cancellationToken == CancellationToken.None
                ? MultiGeneratorTestHelper.RunGenerators(compilation, _generators.ToArray())
                : MultiGeneratorTestHelper.RunGenerators(compilation, _cancellationToken, _generators.ToArray());
        }

        return new MultiGeneratorTestHelper.MultiGeneratorResult
        {
            InputCompilation = compilation,
            OutputCompilation = compilation,
            GeneratorResults = new Dictionary<string, GeneratorRunResult>(StringComparer.Ordinal),
            AllDiagnostics = [.. compilation.GetDiagnostics()]
        };
    }

    private GeneratorPipelineResult CreateResult(Compilation compilation, MultiGeneratorTestHelper.MultiGeneratorResult generatorResult)
    {
        if (_verifyNoErrors)
        {
            var errors = generatorResult.AllDiagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            if (errors.Count > 0)
            {
                var errorMessages = string.Join("\n", errors);
                throw new InvalidOperationException($"Pipeline produced errors:\n{errorMessages}");
            }
        }

        return new GeneratorPipelineResult
        {
            InputCompilation = compilation,
            OutputCompilation = generatorResult.OutputCompilation,
            GeneratorResult = generatorResult,
            RegisteredServices = new Dictionary<string, object>(_services, StringComparer.Ordinal)
        };
    }

    private void RunAssertions(GeneratorPipelineResult result)
    {
        foreach (var assertion in _assertions)
        {
            assertion(result);
        }
    }

}