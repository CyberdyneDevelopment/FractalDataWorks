using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.Conventions.Analyzers.DataGatewayOnlyAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// FDW052 (FDW-3579): ADO.NET connections and commands are created and executed only in an assembly that declares
/// <c>[assembly: Fdw.Conventions.DataAccessAdapter]</c>.
/// </summary>
public class DataGatewayOnlyAnalyzerTests
{
    private const string AdapterAttribute = """
        namespace Fdw.Conventions
        {
            [System.AttributeUsage(System.AttributeTargets.Assembly)]
            public sealed class DataAccessAdapterAttribute : System.Attribute { }
        }

        """;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task CreatingAConnectionStringBuilderOutsideAnAdapterReports()
    {
        var test = AdapterAttribute + """
            public class Reader
            {
                public string Database(string text)
                {
                    var builder = {|#0:new System.Data.Common.DbConnectionStringBuilder()|};
                    builder.ConnectionString = text;
                    return builder.ToString();
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW052").WithLocation(0).WithArguments("DbConnectionStringBuilder"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task ExecutingACommandOutsideAnAdapterReports()
    {
        var test = AdapterAttribute + """
            public class Runner
            {
                public async System.Threading.Tasks.Task<int> Run(System.Data.Common.DbCommand command)
                {
                    using var reader = await {|#0:command.ExecuteReaderAsync()|};
                    return {|#1:command.ExecuteNonQuery()|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW052").WithLocation(0).WithArguments("DbCommand.ExecuteReaderAsync"),
            VerifyCS.Diagnostic("FDW052").WithLocation(1).WithArguments("DbCommand.ExecuteNonQuery"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task AnAssemblyThatDeclaresItselfAnAdapterIsSilent()
    {
        var test = "[assembly: Fdw.Conventions.DataAccessAdapter]\n\n" + AdapterAttribute + """
            public class Adapter
            {
                public int Run(System.Data.Common.DbCommand command)
                {
                    var builder = new System.Data.Common.DbConnectionStringBuilder();
                    builder.ConnectionString = "Data Source=x";
                    return command.ExecuteNonQuery();
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task UsingACommandWithoutCreatingOrExecutingItIsSilent()
    {
        var test = AdapterAttribute + """
            public class Binder
            {
                public void Bind(System.Data.Common.DbCommand command, string text)
                {
                    command.CommandText = text;
                    var builder = new System.Text.StringBuilder();
                    builder.Append(text);
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}
