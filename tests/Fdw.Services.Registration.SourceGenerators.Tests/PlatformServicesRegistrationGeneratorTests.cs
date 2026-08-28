using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Services.Registration.SourceGenerators.Tests;

[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public sealed class PlatformServicesRegistrationGeneratorTests
{
    private const string ValidCollectionSource = """
        using System;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Hosting;
        using Microsoft.Extensions.Logging;
        using Fdw.Collections;

        namespace TestDomain;

        [ServiceTypeCollection(typeof(object), typeof(object), typeof(WidgetTypes), ServiceCategory = "Widget")]
        public static class WidgetTypes
        {
            public static TBuilder Configure<TBuilder>(TBuilder builder, ILoggerFactory? loggerFactory = null)
                where TBuilder : IHostApplicationBuilder
                => builder;

            public static void Register(IServiceCollection services, ILoggerFactory? loggerFactory = null) { }

            public static void Initialize(IServiceProvider services, ILoggerFactory? loggerFactory = null) { }
        }
        """;

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData(false)]
    [InlineData(true)]
    public void NeverWrapsOrElevatesInitializeRegardlessOfAuthAbstractionsVisibility(bool authAbstractionsVisible)
    {
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(
            ValidCollectionSource,
            additionalReferences: authAbstractionsVisible
                ?
                [
                    MetadataReference.CreateFromFile(
                        typeof(Fdw.Services.Authentication.Abstractions.Security.SystemAuthenticationContextScope).Assembly.Location),
                ]
                : []);

        diagnostics.ShouldBeEmpty();
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "PlatformServicesRegistration.g.cs");
        generated.ShouldNotBeNull();

        // Initialize is always the bare method group — no wrapping lambda, no service resolution.
        generated.ShouldContain("TestDomain.WidgetTypes.Initialize));");
        generated.ShouldNotContain("SystemAuthenticationContextScope");
        generated.ShouldNotContain("IAuthenticationContextAccessor");
        generated.ShouldNotContain("GetRequiredService");
    }

    [Fact]
    public void EmitsModuleInitializerAndExtensionForValidCollection()
    {
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(ValidCollectionSource);

        diagnostics.ShouldBeEmpty();

        var generated = compilation.SyntaxTrees
            .Select(t => t.GetText().ToString())
            .FirstOrDefault(t => t.Contains("PlatformServicesRegistration"));

        generated.ShouldNotBeNull();
        generated.ShouldContain("[ModuleInitializer]");
        generated.ShouldContain("internal static PlatformServiceEntry? _widgetEntry;");
        generated.ShouldContain("_widgetEntry = PlatformServices.Add(");
        generated.ShouldContain("\"Widget\"");
        generated.ShouldContain("typeof(TestDomain.WidgetTypes)");
        generated.ShouldContain("TestDomain.WidgetTypes.Configure,");
        generated.ShouldContain("TestDomain.WidgetTypes.Register,");
        generated.ShouldContain("TestDomain.WidgetTypes.Initialize));");
        generated.ShouldContain("public static PlatformServiceEntry Widget");
        generated.ShouldContain("=> PlatformServicesRegistration._widgetEntry");
        // No name-based lookup surface — dot-walk only (no actual ByName(...) call emitted).
        generated.ShouldNotContain(".ByName(");
    }

    [Fact]
    public void EmitsRegistrationUnconditionallyEvenWhenPhaseMethodsAreMissing()
    {
        const string source = """
            using Fdw.Collections;

            namespace TestDomain;

            [ServiceTypeCollection(typeof(object), typeof(object), typeof(BrokenTypes), ServiceCategory = "Broken")]
            public static class BrokenTypes
            {
                // Missing Configure/Register/Initialize entirely.
            }
            """;

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generated = compilation.SyntaxTrees
            .Select(t => t.GetText().ToString())
            .FirstOrDefault(t => t.Contains("PlatformServicesRegistration"));

        generated.ShouldNotBeNull();
        generated.ShouldContain("BrokenTypes");
    }

    [Fact]
    public void EmitsNothingForLibraryOutputKind()
    {
        var (compilation, _) = CompilationHelper.RunGenerator(ValidCollectionSource, outputKind: OutputKind.DynamicallyLinkedLibrary);

        var generated = compilation.SyntaxTrees
            .Select(t => t.GetText().ToString())
            .FirstOrDefault(t => t.Contains("PlatformServicesRegistration"));

        generated.ShouldBeNull();
    }
}
