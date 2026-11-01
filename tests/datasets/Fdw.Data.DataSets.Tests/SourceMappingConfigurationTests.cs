using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

public class SourceMappingConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new SourceMappingConfiguration();

        // Assert
        config.ConnectionType.ShouldBe(string.Empty);
        config.Priority.ShouldBe(100);
        config.Sql.ShouldBeNull();
        config.Http.ShouldBeNull();
        config.File.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ConnectionType_CanBeSet()
    {
        // Arrange
        var config = new SourceMappingConfiguration { ConnectionType = "SQL" };

        // Assert
        config.ConnectionType.ShouldBe("SQL");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Priority_CanBeSet()
    {
        // Arrange
        var config = new SourceMappingConfiguration { Priority = 1 };

        // Assert
        config.Priority.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Sql_CanBeSet()
    {
        // Arrange
        var sqlConfig = new SqlMappingConfiguration { TableName = "Users" };
        var config = new SourceMappingConfiguration { Sql = sqlConfig };

        // Assert
        config.Sql.ShouldNotBeNull();
        config.Sql.TableName.ShouldBe("Users");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Http_CanBeSet()
    {
        // Arrange
        var httpConfig = new HttpMappingConfiguration { Endpoint = "/api/users" };
        var config = new SourceMappingConfiguration { Http = httpConfig };

        // Assert
        config.Http.ShouldNotBeNull();
        config.Http.Endpoint.ShouldBe("/api/users");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void File_CanBeSet()
    {
        // Arrange
        var fileConfig = new FileMappingConfiguration { PathPattern = "data/*.csv" };
        var config = new SourceMappingConfiguration { File = fileConfig };

        // Assert
        config.File.ShouldNotBeNull();
        config.File.PathPattern.ShouldBe("data/*.csv");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange
        var config = new SourceMappingConfiguration
        {
            ConnectionType = "SQL",
            Priority = 1,
            Sql = new SqlMappingConfiguration { TableName = "Users" }
        };

        // Assert
        config.ConnectionType.ShouldBe("SQL");
        config.Priority.ShouldBe(1);
        config.Sql.ShouldNotBeNull();
    }
}
