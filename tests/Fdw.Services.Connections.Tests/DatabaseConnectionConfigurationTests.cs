using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// What only a database connection's configuration states (FDW-1152): the most rows an insert runs serially before it takes the
/// bulk route. It is stated, never defaulted.
/// </summary>
public sealed class DatabaseConnectionConfigurationTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADatabaseConnectionConfigurationStatesMaxSerialInsert()
    {
        new MsSqlConnectionConfiguration { MaxSerialInsert = 500 }
            .ShouldBeAssignableTo<IDatabaseConnectionConfiguration>().ShouldNotBeNull()
            .MaxSerialInsert.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MaxSerialInsertIsNeverDefaulted()
    {
        new MsSqlConnectionConfiguration().MaxSerialInsert.ShouldBe(0);
    }
}
