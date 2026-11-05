using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// FDW053 (FDW-3579): above the connection layer, nothing tests or names a concrete connection, connection type or
/// connection implementation configuration from another assembly.
/// </summary>
public class ConnectionTypeVisibilityAnalyzerTests
{
    private const string ConnectionsAssembly = """
        namespace Fdw.Services.Connections.Abstractions
        {
            public interface IGenericConnection { }
            public interface IConnectionType { }
            public interface IConnectionImplementationConfiguration { }
        }

        namespace Connections
        {
            public sealed class MsSqlConnection : Fdw.Services.Connections.Abstractions.IGenericConnection { }
            public sealed class MsSqlConnectionType : Fdw.Services.Connections.Abstractions.IConnectionType { }
            public sealed class MsSqlConnectionConfiguration : Fdw.Services.Connections.Abstractions.IConnectionImplementationConfiguration { }
            public abstract class ConnectionConfigurationBase : Fdw.Services.Connections.Abstractions.IConnectionImplementationConfiguration { }
        }
        """;

    private static CSharpAnalyzerTest<ConnectionTypeVisibilityAnalyzer, DefaultVerifier> Test(string source)
    {
        var test = new CSharpAnalyzerTest<ConnectionTypeVisibilityAnalyzer, DefaultVerifier> { TestCode = source };
        test.TestState.AdditionalProjects["Connections"].Sources.Add(ConnectionsAssembly);
        test.TestState.AdditionalProjectReferences.Add("Connections");
        return test;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task TestingOrNamingAConnectionTypeFromAnotherAssemblyReports()
    {
        var test = Test("""
            public class Endpoint
            {
                public string Kind(Fdw.Services.Connections.Abstractions.IGenericConnection connection, object configuration)
                {
                    if ({|#0:connection is Connections.MsSqlConnection|})
                        return "mssql";
                    var typed = {|#1:configuration as Connections.MsSqlConnectionConfiguration|};
                    var type = {|#2:typeof(Connections.MsSqlConnectionType)|};
                    return typed is null ? type.Name : "configured";
                }
            }
            """);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("FDW053", Microsoft.CodeAnalysis.DiagnosticSeverity.Error).WithLocation(0).WithArguments("MsSqlConnection"));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("FDW053", Microsoft.CodeAnalysis.DiagnosticSeverity.Error).WithLocation(1).WithArguments("MsSqlConnectionConfiguration"));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("FDW053", Microsoft.CodeAnalysis.DiagnosticSeverity.Error).WithLocation(2).WithArguments("MsSqlConnectionType"));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task AbstractionsAndAbstractBasesAreSilent()
    {
        var test = Test("""
            public class Endpoint
            {
                public bool Check(object value)
                {
                    return value is Fdw.Services.Connections.Abstractions.IGenericConnection
                        || value is Connections.ConnectionConfigurationBase;
                }
            }
            """);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task AnAdapterAssemblyMayNameAnotherAdaptersTypes()
    {
        var test = Test("""
            [assembly: Fdw.Conventions.DataAccessAdapter]

            namespace Fdw.Conventions
            {
                [System.AttributeUsage(System.AttributeTargets.Assembly)]
                public sealed class DataAccessAdapterAttribute : System.Attribute { }
            }

            public class Translator
            {
                public bool Handles(object connection) => connection is Connections.MsSqlConnection;
            }
            """);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task AnAssemblyDeclaringItsOwnConnectionTypeMayNameItsSiblingsTypes()
    {
        var test = Test("""
            public sealed class RegisteredConnectionType : Fdw.Services.Connections.Abstractions.IConnectionType { }

            public class Provider
            {
                public bool Handles(object configuration) => configuration is Connections.MsSqlConnectionConfiguration;
            }
            """);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task HoldingOrPassingAConnectionTypeIsSilent()
    {
        var test = Test("""
            public class Holder
            {
                private readonly Connections.MsSqlConnectionConfiguration _configuration = new();

                public Connections.MsSqlConnectionConfiguration Configuration => _configuration;
            }
            """);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
