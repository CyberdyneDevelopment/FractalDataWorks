using System.Linq;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Connections.MsSql.Validation;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// A database connection may state, in its configuration, how many parameters a key list carries before it goes to a temporary table (FDW-4267); it states none by default, and a number it
/// states must be positive.
/// </summary>
public sealed class DatabaseConnectionConfigurationMaxParametersBeforeTempTableTests
{
    private static string[] Errors(int? maxParametersBeforeTempTable)
        => new MsSqlConnectionConfigurationValidator()
            .Validate(new MsSqlConnectionConfiguration { MaxParametersBeforeTempTable = maxParametersBeforeTempTable })
            .Errors
            .Where(error => error.PropertyName == nameof(MsSqlConnectionConfiguration.MaxParametersBeforeTempTable))
            .Select(error => error.ErrorMessage)
            .ToArray();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ADatabaseConnectionConfigurationStatesNoThresholdByDefault()
    {
        new MsSqlConnectionConfiguration().MaxParametersBeforeTempTable.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TheThresholdIsReadThroughTheDatabaseConfigurationInterface()
    {
        IDatabaseConnectionConfiguration configuration = new MsSqlConnectionConfiguration { MaxParametersBeforeTempTable = 800 };

        configuration.MaxParametersBeforeTempTable.ShouldBe(800);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AConnectionThatStatesNoThresholdIsNotRefusedForIt()
    {
        Errors(null).ShouldBeEmpty();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    [InlineData(0)]
    [InlineData(-5)]
    public void AStatedThresholdThatIsNotPositiveIsRefusedNamingTheProperty(int maxParametersBeforeTempTable)
    {
        Errors(maxParametersBeforeTempTable).ShouldHaveSingleItem().ShouldContain("MaxParametersBeforeTempTable must be greater than 0");
    }
}
