using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.ServiceTypes.Analyzers.ServiceProviderInjectionAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.ServiceTypes.Analyzers.Tests;

public class ServiceProviderInjectionAnalyzerTests
{
    // Why: stand-ins for the real Fdw.Services.Abstractions.IServiceOption /
    // Fdw.ServiceTypes.IFdwServiceProvider<...> types — the test project references only the analyzer
    // assembly, not the framework, so each test source declares minimal shapes that satisfy the
    // analyzer's semantic (AllInterfaces-based) checks.
    private const string CommonScaffolding = """
        namespace Fdw.Abstractions
        {
            public interface IGenericService
            {
            }
        }

        namespace Fdw.Services.Abstractions
        {
            using Fdw.Abstractions;
            using System;

            public interface IServiceOption : IGenericService
            {
            }

            [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
            public sealed class ServiceOptionDependencyAttribute : Attribute
            {
            }
        }

        namespace Fdw.ServiceTypes
        {
            using Fdw.Abstractions;

            public interface IFdwServiceProvider<TService> where TService : IGenericService
            {
            }

            public interface IFdwServiceProvider<TService, TConfiguration> : IFdwServiceProvider<TService>
                where TService : IGenericService
            {
            }
        }

        namespace TestNamespace
        {
            using Fdw.Services.Abstractions;

            public interface ISecretManager : IServiceOption
            {
            }

            public class SecretManagerConfiguration
            {
            }
        }

        """;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ServiceOptionService_InjectsAnotherServiceOptionDirectly_ReportsDiagnostic()
    {
        var test = CommonScaffolding + """
            namespace TestNamespace
            {
                using Fdw.Services.Abstractions;

                public interface IEtlPipeline : IServiceOption
                {
                }

                public class EtlPipeline : IEtlPipeline
                {
                    public EtlPipeline(ISecretManager {|#0:secretManager|})
                    {
                    }
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW044")
            .WithLocation(0)
            .WithArguments("EtlPipeline", "ISecretManager");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ServiceOptionService_InjectsProvider_NoDiagnostic()
    {
        var test = CommonScaffolding + """
            namespace TestNamespace
            {
                using Fdw.Services.Abstractions;
                using Fdw.ServiceTypes;

                public interface IEtlPipeline : IServiceOption
                {
                }

                public class EtlPipeline : IEtlPipeline
                {
                    public EtlPipeline(IFdwServiceProvider<ISecretManager, SecretManagerConfiguration> secretManagerProvider)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task PlainClass_InjectsServiceOptionDirectly_NoDiagnostic()
    {
        var test = CommonScaffolding + """
            namespace TestNamespace
            {
                public class PlainConsumer
                {
                    public PlainConsumer(ISecretManager secretManager)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task DerivedServiceOptionInterface_ClassInjectsServiceOptionDirectly_ReportsDiagnostic()
    {
        // Why: proves transitivity — IDerived is never marked with IServiceOption itself, it only
        // extends an interface that IS marked. AllInterfaces must still catch it.
        var test = CommonScaffolding + """
            namespace TestNamespace
            {
                using Fdw.Services.Abstractions;

                public interface IEtlPipeline : IServiceOption
                {
                }

                public interface IDerived : IEtlPipeline
                {
                }

                public class DerivedPipeline : IDerived
                {
                    public DerivedPipeline(ISecretManager {|#0:secretManager|})
                    {
                    }
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW044")
            .WithLocation(0)
            .WithArguments("DerivedPipeline", "ISecretManager");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ServiceOptionService_InjectsParameterTypedAsDerivedServiceOptionInterface_ReportsDiagnostic()
    {
        // Why: proves transitivity on the injected-parameter side — the parameter is typed as
        // IDerived (which is never marked directly), not as the marked ISecretManager itself.
        var test = CommonScaffolding + """
            namespace TestNamespace
            {
                using Fdw.Services.Abstractions;

                public interface IDerived : ISecretManager
                {
                }

                public interface IEtlPipeline : IServiceOption
                {
                }

                public class EtlPipeline : IEtlPipeline
                {
                    public EtlPipeline(IDerived {|#0:derived|})
                    {
                    }
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW044")
            .WithLocation(0)
            .WithArguments("EtlPipeline", "IDerived");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ServiceOptionService_InjectsServiceOptionWithOptOutAttribute_NoDiagnostic()
    {
        var test = CommonScaffolding + """
            namespace TestNamespace
            {
                using Fdw.Services.Abstractions;

                public interface IEtlPipeline : IServiceOption
                {
                }

                public class EtlPipeline : IEtlPipeline
                {
                    public EtlPipeline([ServiceOptionDependency] ISecretManager secretManager)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ServiceOptionService_InjectsServiceOptionWithoutOptOutAttribute_ReportsDiagnostic()
    {
        // Why: same shape as the opt-out test above but without [ServiceOptionDependency] — proves the
        // attribute (not some other property of the parameter) is what suppresses the diagnostic.
        var test = CommonScaffolding + """
            namespace TestNamespace
            {
                using Fdw.Services.Abstractions;

                public interface IEtlPipeline : IServiceOption
                {
                }

                public class EtlPipeline : IEtlPipeline
                {
                    public EtlPipeline(ISecretManager {|#0:secretManager|})
                    {
                    }
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW044")
            .WithLocation(0)
            .WithArguments("EtlPipeline", "ISecretManager");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }
}
