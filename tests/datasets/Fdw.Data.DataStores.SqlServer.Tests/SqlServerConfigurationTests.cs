using Fdw.Data.DataStores.SqlServer;
using Shouldly;

namespace Fdw.Data.DataStores.SqlServer.Tests;

public sealed class SqlServerConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConnectionString_ShouldBeSettable()
    {
        // Arrange
        var config = new SqlServerConfiguration();
        var connectionString = "Server=localhost;Database=test;";

        // Act
        config.ConnectionString = connectionString;

        // Assert
        config.ConnectionString.ShouldBe(connectionString);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void UseAzureAuthentication_ShouldBeSettable()
    {
        // Arrange
        var config = new SqlServerConfiguration();

        // Act
        config.UseAzureAuthentication = true;

        // Assert
        config.UseAzureAuthentication.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllProperties_ShouldBeIndependent()
    {
        // Arrange
        var config = new SqlServerConfiguration
        {
            ConnectionString = "Server=test;",
            UseAzureAuthentication = true
        };

        // Assert
        config.ConnectionString.ShouldBe("Server=test;");
        config.UseAzureAuthentication.ShouldBeTrue();
    }
}
