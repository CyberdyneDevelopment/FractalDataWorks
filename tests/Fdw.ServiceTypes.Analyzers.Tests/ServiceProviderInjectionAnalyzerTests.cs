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

            public interface IPlatformServiceProvider<TService> where TService : IGenericService
            {
            }

            public interface IPlatformServiceProvider<TService, TConfiguration> : IPlatformServiceProvider<TService>
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
                    public EtlPipeline(IPlatformServiceProvider<ISecretManager, SecretManagerConfiguration> secretManagerProvider)
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
