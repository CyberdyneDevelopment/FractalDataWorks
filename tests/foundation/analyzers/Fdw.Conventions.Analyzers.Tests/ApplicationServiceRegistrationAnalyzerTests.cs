using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// FDW056 (FDW-3582): an application registers no service itself; registration goes through FDW's
/// ServiceTypeCollections and the PlatformServices phases.
/// </summary>
public class ApplicationServiceRegistrationAnalyzerTests
{
    /// <summary>
    /// The DI and hosting stand-ins, compiled as a separate library the test project references, as the real
    /// Microsoft.Extensions packages are. Declared in the test's own executable, their Add* extensions would be
    /// application-declared extensions and FDW056 would report them.
    /// </summary>
    private const string Library = """
        namespace Microsoft.Extensions.DependencyInjection
        {
            public sealed class ServiceDescriptor { }

            public interface IServiceCollection : System.Collections.Generic.ICollection<ServiceDescriptor> { }

            public static class ServiceCollectionServiceExtensions
            {
                public static IServiceCollection AddSingleton<TService, TImplementation>(this IServiceCollection services)
                    where TImplementation : TService => services;

                public static IServiceCollection TryAddScoped<TService>(this IServiceCollection services) => services;
            }
        }

        namespace Microsoft.Extensions.Hosting
        {
            public interface IHostApplicationBuilder
            {
                Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
            }
        }

        namespace Fdw
        {
            public static class PlatformServices
            {
                public static bool Register(Microsoft.Extensions.Hosting.IHostApplicationBuilder builder) => true;
            }
        }

        public interface IClock { }

        public sealed class Clock : IClock { }
        """;

    private const string Support = """
        public static class Program
        {
            public static void Main() { }
        }

        """;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task RegistrationCallsInAnApplicationReport()
    {
        var test = Support + """
            public static class Startup
            {
                public static void Wire(Microsoft.Extensions.Hosting.IHostApplicationBuilder builder)
                {
                    {|#0:Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<IClock, Clock>(builder.Services)|};
                    {|#1:Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.TryAddScoped<IClock>(builder.Services)|};
                    {|#2:builder.Services.Add(new Microsoft.Extensions.DependencyInjection.ServiceDescriptor())|};
                }
            }
            """;

        await Verify(
            test,
            OutputKind.ConsoleApplication,
            isTestProject: false,
            new DiagnosticResult("FDW056", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("'AddSingleton' registers a service in an application"),
            new DiagnosticResult("FDW056", DiagnosticSeverity.Error)
                .WithLocation(1)
                .WithArguments("'TryAddScoped' registers a service in an application"),
            new DiagnosticResult("FDW056", DiagnosticSeverity.Error)
                .WithLocation(2)
                .WithArguments("'Add' registers a service in an application"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task AnAddExtensionDeclaredInAnApplicationReports()
    {
        var test = Support + """
            public static class HostExtensions
            {
                public static Microsoft.Extensions.DependencyInjection.IServiceCollection {|#0:AddClock|}(
                    this Microsoft.Extensions.DependencyInjection.IServiceCollection services) => services;

                public static Microsoft.Extensions.Hosting.IHostApplicationBuilder {|#1:AddDefaults|}(
                    this Microsoft.Extensions.Hosting.IHostApplicationBuilder builder) => builder;
            }
            """;

        await Verify(
            test,
            OutputKind.ConsoleApplication,
            isTestProject: false,
            new DiagnosticResult("FDW056", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("'AddClock' is an Add* extension on IServiceCollection declared in an application"),
            new DiagnosticResult("FDW056", DiagnosticSeverity.Error)
                .WithLocation(1)
                .WithArguments("'AddDefaults' is an Add* extension on IHostApplicationBuilder declared in an application"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task PlatformServicesInAnApplicationIsSilent()
    {
        var test = Support + """
            public static class Startup
            {
                public static bool Wire(Microsoft.Extensions.Hosting.IHostApplicationBuilder builder) =>
                    Fdw.PlatformServices.Register(builder);
            }
            """;

        await Verify(test, OutputKind.ConsoleApplication, isTestProject: false);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task ALibraryIsSilent()
    {
        var test = Support + """
            public static class HostExtensions
            {
                public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddClock(
                    this Microsoft.Extensions.DependencyInjection.IServiceCollection services) =>
                    Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<IClock, Clock>(services);
            }
            """;

        await Verify(test, OutputKind.DynamicallyLinkedLibrary, isTestProject: false);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task ATestProjectIsSilent()
    {
        var test = Support + """
            public static class Fixture
            {
                public static void Wire(Microsoft.Extensions.DependencyInjection.IServiceCollection services) =>
                    Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<IClock, Clock>(services);
            }
            """;

        await Verify(test, OutputKind.ConsoleApplication, isTestProject: true);
    }

    private static async Task Verify(string source, OutputKind outputKind, bool isTestProject, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<ApplicationServiceRegistrationAnalyzer, DefaultVerifier>
        {
            TestCode = source,
        };

        test.TestState.OutputKind = outputKind;
        test.TestState.AdditionalProjects["StandIns"].Sources.Add(("/StandIns/StandIns.cs", Library));
        test.TestState.AdditionalProjectReferences.Add("StandIns");
        test.TestState.AnalyzerConfigFiles.Add((
            "/.globalconfig",
            $"is_global = true\nbuild_property.IsTestProject = {(isTestProject ? "true" : "false")}\n"));
        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync();
    }
}
