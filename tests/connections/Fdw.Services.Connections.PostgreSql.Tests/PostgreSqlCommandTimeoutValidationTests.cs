using System.Linq;
using Fdw.Services.Connections.PostgreSql;
using Fdw.Services.Connections.PostgreSql.Validation;

namespace Fdw.Services.Connections.PostgreSql.Tests;

/// <summary>
/// A connection may state no command timeout (FDW-3284): the instance's setting then gives it one. A timeout it does state must be positive.
/// </summary>
public sealed class PostgreSqlCommandTimeoutValidationTests
{
    private static string[] TimeoutErrors(int? commandTimeout)
        => new PostgreSqlConnectionConfigurationValidator()
            .Validate(new PostgreSqlConnectionConfiguration { CommandTimeout = commandTimeout })
            .Errors
            .Where(error => error.PropertyName == nameof(PostgreSqlConnectionConfiguration.CommandTimeout))
            .Select(error => error.ErrorMessage)
            .ToArray();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ANewConfigurationStatesNoCommandTimeout()
    {
        new PostgreSqlConnectionConfiguration().CommandTimeout.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AConnectionThatStatesNoCommandTimeoutIsNotRefusedForIt()
    {
        TimeoutErrors(null).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AStatedCommandTimeoutOfFortyFiveIsAccepted()
    {
        TimeoutErrors(45).ShouldBeEmpty();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    [InlineData(0)]
    [InlineData(-1)]
    public void AStatedCommandTimeoutThatIsNotPositiveIsRefusedNamingTheProperty(int commandTimeout)
    {
        var errors = TimeoutErrors(commandTimeout);

        errors.ShouldHaveSingleItem().ShouldContain("CommandTimeout must be greater than 0");
    }
}
