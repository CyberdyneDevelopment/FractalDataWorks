using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.Conventions.Analyzers.TopLevelConfigurationWriteAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// FDW054 (FDW-3579): a top-level configuration is written only through its domain configuration provider; child
/// records may still be written with ConfigurationSaveCommand.
/// </summary>
public class TopLevelConfigurationWriteAnalyzerTests
{
    private const string Framework = """
        namespace Fdw.Configuration
        {
            public interface IGenericConfiguration { }
            public interface IImplementationConfiguration : IGenericConfiguration { }
            public interface IDomainConfiguration : IGenericConfiguration { }
        }

        namespace Fdw.Commands.Data
        {
            public abstract class ConfigurationWriteCommand<TInput> { }
            public sealed class ConfigurationSaveCommand<T> : ConfigurationWriteCommand<T> where T : class
            {
                public ConfigurationSaveCommand(T value) { }
            }
        }

        namespace Fdw.Services.Configuration
        {
            public abstract class DomainConfigurationProviderBase<TImplementationConfiguration> { }
        }

        public sealed class MsSqlConnectionConfiguration : Fdw.Configuration.IImplementationConfiguration { }
        public sealed class DataSetSampleRowConfiguration { }

        """;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task SavingATopLevelConfigurationOutsideItsProviderReports()
    {
        var test = Framework + """
            public static class EndpointHelper
            {
                public static object Save(MsSqlConnectionConfiguration configuration)
                    => {|#0:new Fdw.Commands.Data.ConfigurationSaveCommand<MsSqlConnectionConfiguration>(configuration)|};
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW054").WithLocation(0).WithArguments("MsSqlConnectionConfiguration"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task SavingAChildRecordIsSilent()
    {
        var test = Framework + """
            public static class SampleWriter
            {
                public static object Save(DataSetSampleRowConfiguration row)
                    => new Fdw.Commands.Data.ConfigurationSaveCommand<DataSetSampleRowConfiguration>(row);
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task TheProviderMaySaveTheTopLevelConfiguration()
    {
        var test = Framework + """
            public sealed class ConnectionProvider
                : Fdw.Services.Configuration.DomainConfigurationProviderBase<MsSqlConnectionConfiguration>
            {
                public object Save(MsSqlConnectionConfiguration configuration)
                    => new Fdw.Commands.Data.ConfigurationSaveCommand<MsSqlConnectionConfiguration>(configuration);
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task AnOpenTypeParameterIsSilent()
    {
        var test = Framework + """
            public static class Store
            {
                public static object Save<T>(T value) where T : class, Fdw.Configuration.IImplementationConfiguration
                    => new Fdw.Commands.Data.ConfigurationSaveCommand<T>(value);
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}
