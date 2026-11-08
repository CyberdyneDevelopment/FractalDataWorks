using System.Linq;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.PostgreSql;
using Fdw.Services.Connections.PostgreSql.Validation;

namespace Fdw.Services.Connections.PostgreSql.Tests;

/// <summary>
/// A connection may state how many parameters a key list carries before it goes to a temporary table, or state nothing (FDW-4267). A number it does state must be positive.
/// </summary>
public sealed class PostgreSqlMaxParametersBeforeTempTableValidationTests
{
    private static string[] Errors(int? maxParametersBeforeTempTable)
        => new PostgreSqlConnectionConfigurationValidator()
            .Validate(new PostgreSqlConnectionConfiguration { MaxParametersBeforeTempTable = maxParametersBeforeTempTable })
            .Errors
            .Where(error => error.PropertyName == nameof(PostgreSqlConnectionConfiguration.MaxParametersBeforeTempTable))
            .Select(error => error.ErrorMessage)
            .ToArray();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ANewConfigurationStatesNoThresholdAndIsADatabaseConfiguration()
    {
        var configuration = new PostgreSqlConnectionConfiguration();

        configuration.MaxParametersBeforeTempTable.ShouldBeNull();
        configuration.ShouldBeAssignableTo<IDatabaseConnectionConfiguration>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AConnectionThatStatesNoThresholdIsNotRefusedForIt()
    {
        Errors(null).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AStatedThresholdIsAccepted()
    {
        Errors(500).ShouldBeEmpty();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    [InlineData(0)]
    [InlineData(-1)]
    public void AStatedThresholdThatIsNotPositiveIsRefusedNamingTheProperty(int maxParametersBeforeTempTable)
    {
        Errors(maxParametersBeforeTempTable).ShouldHaveSingleItem().ShouldContain("MaxParametersBeforeTempTable must be greater than 0");
    }
}
