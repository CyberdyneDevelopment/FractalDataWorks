using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

public class SqlMappingConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new SqlMappingConfiguration();

        // Assert
        config.SchemaName.ShouldBeNull();
        config.TableName.ShouldBe(string.Empty);
        config.FieldMappings.ShouldNotBeNull();
        config.FieldMappings.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void SchemaName_CanBeSet()
    {
        // Arrange
        var config = new SqlMappingConfiguration { SchemaName = "dbo" };

        // Assert
        config.SchemaName.ShouldBe("dbo");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TableName_CanBeSet()
    {
        // Arrange
        var config = new SqlMappingConfiguration { TableName = "Users" };

        // Assert
        config.TableName.ShouldBe("Users");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void FieldMappings_CanBePopulated()
    {
        // Arrange
        var config = new SqlMappingConfiguration();

        // Act
        config.FieldMappings["FieldName"] = "ColumnName";

        // Assert
        config.FieldMappings.Count.ShouldBe(1);
        config.FieldMappings["FieldName"].ShouldBe("ColumnName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void FieldMappings_IsCaseInsensitive()
    {
        // Arrange
        var config = new SqlMappingConfiguration();

        // Act
        config.FieldMappings["FieldName"] = "ColumnName";

        // Assert
        config.FieldMappings["fieldname"].ShouldBe("ColumnName");
        config.FieldMappings["FIELDNAME"].ShouldBe("ColumnName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange
        var config = new SqlMappingConfiguration
        {
            SchemaName = "dbo",
            TableName = "Users"
        };
        config.FieldMappings["Id"] = "user_id";
        config.FieldMappings["Name"] = "user_name";

        // Assert
        config.SchemaName.ShouldBe("dbo");
        config.TableName.ShouldBe("Users");
        config.FieldMappings.Count.ShouldBe(2);
    }
}

public class HttpMappingConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new HttpMappingConfiguration();

        // Assert
        config.Endpoint.ShouldBe(string.Empty);
        config.Method.ShouldBe(string.Empty);
        config.QueryParameters.ShouldNotBeNull();
        config.QueryParameters.ShouldBeEmpty();
        config.FieldMappings.ShouldNotBeNull();
        config.FieldMappings.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Endpoint_CanBeSet()
    {
        // Arrange
        var config = new HttpMappingConfiguration { Endpoint = "/api/users" };

        // Assert
        config.Endpoint.ShouldBe("/api/users");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Method_CanBeSet()
    {
        // Arrange
        var config = new HttpMappingConfiguration { Method = "POST" };

        // Assert
        config.Method.ShouldBe("POST");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void QueryParameters_CanBePopulated()
    {
        // Arrange
        var config = new HttpMappingConfiguration();

        // Act
        config.QueryParameters["page"] = "1";
        config.QueryParameters["size"] = "10";

        // Assert
        config.QueryParameters.Count.ShouldBe(2);
        config.QueryParameters["page"].ShouldBe("1");
        config.QueryParameters["size"].ShouldBe("10");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void QueryParameters_IsCaseInsensitive()
    {
        // Arrange
        var config = new HttpMappingConfiguration();

        // Act
        config.QueryParameters["Page"] = "1";

        // Assert
        config.QueryParameters["page"].ShouldBe("1");
        config.QueryParameters["PAGE"].ShouldBe("1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void FieldMappings_CanBePopulated()
    {
        // Arrange
        var config = new HttpMappingConfiguration();

        // Act
        config.FieldMappings["Id"] = "id";
        config.FieldMappings["Name"] = "fullName";

        // Assert
        config.FieldMappings.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void FieldMappings_IsCaseInsensitive()
    {
        // Arrange
        var config = new HttpMappingConfiguration();

        // Act
        config.FieldMappings["FieldName"] = "jsonField";

        // Assert
        config.FieldMappings["fieldname"].ShouldBe("jsonField");
        config.FieldMappings["FIELDNAME"].ShouldBe("jsonField");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange
        var config = new HttpMappingConfiguration
        {
            Endpoint = "/api/data",
            Method = "POST"
        };
        config.QueryParameters["filter"] = "active";
        config.FieldMappings["Id"] = "id";

        // Assert
        config.Endpoint.ShouldBe("/api/data");
        config.Method.ShouldBe("POST");
        config.QueryParameters.Count.ShouldBe(1);
        config.FieldMappings.Count.ShouldBe(1);
    }
}

public class FileMappingConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new FileMappingConfiguration();

        // Assert
        config.PathPattern.ShouldBe(string.Empty);
        config.Format.ShouldBe(string.Empty);
        config.FieldMappings.ShouldNotBeNull();
        config.FieldMappings.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void PathPattern_CanBeSet()
    {
        // Arrange
        var config = new FileMappingConfiguration { PathPattern = "data/*.csv" };

        // Assert
        config.PathPattern.ShouldBe("data/*.csv");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Format_CanBeSet()
    {
        // Arrange
        var config = new FileMappingConfiguration { Format = "CSV" };

        // Assert
        config.Format.ShouldBe("CSV");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void FieldMappings_CanBePopulated()
    {
        // Arrange
        var config = new FileMappingConfiguration();

        // Act
        config.FieldMappings["Id"] = "ID_Column";
        config.FieldMappings["Name"] = "NAME_Column";

        // Assert
        config.FieldMappings.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void FieldMappings_IsCaseInsensitive()
    {
        // Arrange
        var config = new FileMappingConfiguration();

        // Act
        config.FieldMappings["FieldName"] = "Column";

        // Assert
        config.FieldMappings["fieldname"].ShouldBe("Column");
        config.FieldMappings["FIELDNAME"].ShouldBe("Column");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange
        var config = new FileMappingConfiguration
        {
            PathPattern = "data/{date}/*.json",
            Format = "JSON"
        };
        config.FieldMappings["Id"] = "id";

        // Assert
        config.PathPattern.ShouldBe("data/{date}/*.json");
        config.Format.ShouldBe("JSON");
        config.FieldMappings.Count.ShouldBe(1);
    }
}
