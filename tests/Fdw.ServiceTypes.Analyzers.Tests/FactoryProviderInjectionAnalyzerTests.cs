using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.ServiceTypes.Analyzers.FactoryProviderInjectionAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.ServiceTypes.Analyzers.Tests;

public class FactoryProviderInjectionAnalyzerTests
{
    // Why: minimal stand-ins for Fdw.Abstractions.IServiceFactory,
    // Fdw.ServiceTypes.IFdwServiceProvider<...>, System.Lazy<T>, and IServiceScopeFactory — the test
    // project references only the analyzer assembly, so each source declares the shapes the analyzer's
    // metadata-name lookups need. System.Lazy<T> is redeclared in a test namespace so its metadata name
    // resolves to System.Lazy`1 as the analyzer expects (the real corelib type is also present, but the
    // analyzer keys on the metadata name which both satisfy).
    private const string CommonScaffolding = """
        namespace Fdw.Abstractions
        {
            public interface IGenericService
            {
            }

            public interface IServiceFactory
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

        namespace Microsoft.Extensions.DependencyInjection
        {
            public interface IServiceScopeFactory
            {
            }
        }

        namespace TestNamespace
        {
            using Fdw.Abstractions;

            public interface ISecretManager : IGenericService
            {
            }
        }

        """;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task Factory_InjectsProviderDirectly_ReportsDiagnostic()
    {
        var test = CommonScaffolding + """
            namespace TestNamespace
            {
                using Fdw.Abstractions;
                using Fdw.ServiceTypes;

                public class SqlConnectionFactory : IServiceFactory
                {
                    public SqlConnectionFactory(IFdwServiceProvider<ISecretManager> {|#0:provider|})
                    {
                    }
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW045")
            .WithLocation(0)
            .WithArguments("SqlConnectionFactory", "IFdwServiceProvider");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task Factory_InjectsScopeFactory_ReportsDiagnostic()
    {
        var test = CommonScaffolding + """
            namespace TestNamespace
            {
                using Fdw.Abstractions;
                using Microsoft.Extensions.DependencyInjection;

                public class SqlConnectionFactory : IServiceFactory
                {
                    public SqlConnectionFactory(IServiceScopeFactory {|#0:scopeFactory|})
                    {
                    }
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW045")
            .WithLocation(0)
            .WithArguments("SqlConnectionFactory", "IServiceScopeFactory");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task Factory_InjectsProviderAsLazy_NoDiagnostic()
    {
        var test = CommonScaffolding + """
            namespace TestNamespace
            {
                using System;
                using Fdw.Abstractions;
                using Fdw.ServiceTypes;

                public class SqlConnectionFactory : IServiceFactory
                {
                    public SqlConnectionFactory(Lazy<IFdwServiceProvider<ISecretManager>> provider)
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
    public async Task Factory_PureConstructor_NoDiagnostic()
    {
        var test = CommonScaffolding + """
            namespace TestNamespace
            {
                using Fdw.Abstractions;

                public class SqlConnectionFactory : IServiceFactory
                {
                    public SqlConnectionFactory(string name)
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
    public async Task NonFactory_InjectsProvider_NoDiagnostic()
    {
        // Why: the rule targets IServiceFactory implementors only; an ordinary class may hold a provider.
        var test = CommonScaffolding + """
            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                public class SomeService
                {
                    public SomeService(IFdwServiceProvider<ISecretManager> provider)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}
