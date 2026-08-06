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
    // Why: the analyzer resolves parameter and return types by fully-qualified name, so the test source
    // must declare the real ones. The framework references are not available to the analyzer test host,
    // and stand-ins under the same namespaces are what the production symbols resolve to anyway.
    private const string Scaffold = """
        using System;

        namespace Microsoft.Extensions.Hosting { public interface IHostApplicationBuilder { } }
        namespace Microsoft.Extensions.Logging { public interface ILoggerFactory { } }

        namespace Fdw.Collections.Attributes
        {
            [AttributeUsage(AttributeTargets.Class)]
            public sealed class ServiceTypeCollectionAttribute : Attribute { }

            [AttributeUsage(AttributeTargets.Class)]
            public sealed class PlatformServiceProviderAttribute : Attribute { }
        }
        """;

    private const string AllThreePhases = """
                public static IHostApplicationBuilder Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory) => builder;
                public static IHostApplicationBuilder Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory) => builder;
                public static IServiceProvider Initialize(IServiceProvider services, ILoggerFactory? loggerFactory) => services;
        """;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ServiceTypeCollection_WithAllPhaseMethods_ReportsNothing()
    {
        // Why: the false-positive case is the one that matters — every existing correct collection in the
        // solution has exactly this shape, so a rule that fires here would fail the whole build.
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
                    public static IHostApplicationBuilder Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory) => builder;
                    public static IHostApplicationBuilder Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory) => builder;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            new DiagnosticResult(ServiceTypeCollectionPhaseMethodsAnalyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("MissingInitializeTypes", "IServiceProvider Initialize(IServiceProvider, ILoggerFactory?)"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task PlatformServiceProvider_MissingAllPhases_ReportsThreeDiagnostics()
    {
        // Why: [PlatformServiceProvider] classes are hand-written — the generator emits no half for them,
        // so all three are the author's responsibility and all three must be reported at once rather than
        // one per rebuild.
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
                .WithArguments("BareProvider", "IHostApplicationBuilder Configure(IHostApplicationBuilder, ILoggerFactory?)"),
            new DiagnosticResult(ServiceTypeCollectionPhaseMethodsAnalyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("BareProvider", "IHostApplicationBuilder Register(IHostApplicationBuilder, ILoggerFactory?)"),
            new DiagnosticResult(ServiceTypeCollectionPhaseMethodsAnalyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("BareProvider", "IServiceProvider Initialize(IServiceProvider, ILoggerFactory?)"));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public async Task PhaseMethod_WithWrongShape_ReportsDiagnostic()
    {
        // Why: a method group only converts to the descriptor's delegate if the parameters and return type
        // match. An instance method, or one returning void, compiles fine here but breaks in generated
        // code — which is exactly the failure this rule exists to move to the declaration.
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
                    public static IHostApplicationBuilder Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory) => builder;
                    public static void Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory) { }
                    public static IServiceProvider Initialize(IServiceProvider services, ILoggerFactory? loggerFactory) => services;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            new DiagnosticResult(ServiceTypeCollectionPhaseMethodsAnalyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("WrongShapeTypes", "IHostApplicationBuilder Register(IHostApplicationBuilder, ILoggerFactory?)"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task PhaseMethods_InheritedFromBase_ReportsNothing()
    {
        // Why: THE regression guard. Real collections do not redeclare the phase methods — they inherit
        // them as statics from ServiceTypeCollectionBase, and a C# static is reachable through the derived
        // type name, so the generator's method group binds to the base. Checking declared members only
        // reported every correct collection in the solution as an error.
        var test = $$"""
            {{Scaffold}}

            namespace TestNamespace
            {
                using Microsoft.Extensions.Hosting;
                using Microsoft.Extensions.Logging;
                using Fdw.Collections.Attributes;

                public abstract class CollectionBase
                {
                    public static IHostApplicationBuilder Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null) => builder;
                    public static IHostApplicationBuilder Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null) => builder;
                    public static IServiceProvider Initialize(IServiceProvider services, ILoggerFactory? loggerFactory = null) => services;
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
