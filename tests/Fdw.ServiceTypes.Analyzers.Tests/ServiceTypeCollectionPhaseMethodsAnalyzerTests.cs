using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.ServiceTypes.Analyzers.ServiceTypeCollectionPhaseMethodsAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.ServiceTypes.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="ServiceTypeCollectionPhaseMethodsAnalyzer"/> (FDW024).
/// </summary>
public class ServiceTypeCollectionPhaseMethodsAnalyzerTests
{
    private const string Scaffold = """
        using System;

        namespace Microsoft.Extensions.Hosting { public interface IHostApplicationBuilder { } public interface IHost { } }
        namespace Microsoft.Extensions.Logging { public interface ILoggerFactory { } }
        namespace Fdw.Results { public interface IGenericResult<out T> { } }

        namespace Fdw.Collections.Attributes
        {
            [AttributeUsage(AttributeTargets.Class)]
            public sealed class ServiceTypeCollectionAttribute : Attribute { }

            [AttributeUsage(AttributeTargets.Class)]
            public sealed class PlatformServiceProviderAttribute : Attribute { }
        }
        """;

    private const string AllThreePhases = """
                public static Fdw.Results.IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory) => null!;
                public static Fdw.Results.IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory) => null!;
                public static Fdw.Results.IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory) => null!;
        """;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ServiceTypeCollection_WithAllPhaseMethods_ReportsNothing()
    {
        var test = $$"""
            {{Scaffold}}

            namespace TestNamespace
            {
                using Microsoft.Extensions.Hosting;
                using Microsoft.Extensions.Logging;
                using Fdw.Collections.Attributes;

                [ServiceTypeCollection]
                public partial class GoodTypes
                {
            {{AllThreePhases}}
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ServiceTypeCollection_MissingInitialize_ReportsDiagnostic()
    {
        var test = $$"""
            {{Scaffold}}

            namespace TestNamespace
            {
                using Microsoft.Extensions.Hosting;
                using Microsoft.Extensions.Logging;
                using Fdw.Collections.Attributes;

                [ServiceTypeCollection]
                public partial class {|#0:MissingInitializeTypes|}
                {
                    public static Fdw.Results.IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory) => null!;
                    public static Fdw.Results.IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory) => null!;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            new DiagnosticResult(ServiceTypeCollectionPhaseMethodsAnalyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("MissingInitializeTypes", "IGenericResult<IHost> Initialize(IHost, ILoggerFactory?, bool force = false, bool defer = false)"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task PlatformServiceProvider_MissingAllPhases_ReportsThreeDiagnostics()
    {
        var test = $$"""
            {{Scaffold}}

            namespace TestNamespace
            {
                using Fdw.Collections.Attributes;

                [PlatformServiceProvider]
                public partial class {|#0:BareProvider|}
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            new DiagnosticResult(ServiceTypeCollectionPhaseMethodsAnalyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("BareProvider", "IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder, ILoggerFactory?, bool force = false, bool defer = false)"),
            new DiagnosticResult(ServiceTypeCollectionPhaseMethodsAnalyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("BareProvider", "IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder, ILoggerFactory?, bool force = false, bool defer = false)"),
            new DiagnosticResult(ServiceTypeCollectionPhaseMethodsAnalyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("BareProvider", "IGenericResult<IHost> Initialize(IHost, ILoggerFactory?, bool force = false, bool defer = false)"));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public async Task PhaseMethod_WithWrongShape_ReportsDiagnostic()
    {
        var test = $$"""
            {{Scaffold}}

            namespace TestNamespace
            {
                using Microsoft.Extensions.Hosting;
                using Microsoft.Extensions.Logging;
                using Fdw.Collections.Attributes;

                [ServiceTypeCollection]
                public partial class {|#0:WrongShapeTypes|}
                {
                    public static Fdw.Results.IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory) => null!;
                    public static void Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory) { }
                    public static Fdw.Results.IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory) => null!;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            new DiagnosticResult(ServiceTypeCollectionPhaseMethodsAnalyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("WrongShapeTypes", "IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder, ILoggerFactory?, bool force = false, bool defer = false)"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task PhaseMethods_InheritedFromBase_ReportsNothing()
    {
        var test = $$"""
            {{Scaffold}}

            namespace TestNamespace
            {
                using Microsoft.Extensions.Hosting;
                using Microsoft.Extensions.Logging;
                using Fdw.Collections.Attributes;

                public abstract class CollectionBase
                {
                    public static Fdw.Results.IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null) => null!;
                    public static Fdw.Results.IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null) => null!;
                    public static Fdw.Results.IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null) => null!;
                }

                [ServiceTypeCollection]
                public partial class InheritingTypes : CollectionBase
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public async Task UndecoratedClass_ReportsNothing()
    {
        var test = $$"""
            {{Scaffold}}

            namespace TestNamespace
            {
                public class NotACollection { }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}
