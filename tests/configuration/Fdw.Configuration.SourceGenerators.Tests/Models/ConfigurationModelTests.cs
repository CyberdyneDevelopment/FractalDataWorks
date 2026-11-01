using Fdw.Configuration.SourceGenerators.Models;
using Shouldly;
using Xunit;

namespace Fdw.Configuration.SourceGenerators.Tests.Models;

public class ConfigurationModelTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetEffectiveTableNameReturnsExplicitTableName()
    {
        // Arrange
        var model = new ConfigurationModel
        {
            ClassName = "TestConfiguration",
            TableName = "CustomTable"
        };

        // Act
        var result = model.GetEffectiveTableName();

        // Assert
        result.ShouldBe("CustomTable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetEffectiveTableNameStripsConfigurationSuffix()
    {
        // Arrange
        var model = new ConfigurationModel
        {
            ClassName = "MsSqlConnectionConfiguration"
        };

        // Act
        var result = model.GetEffectiveTableName();

        // Assert
        result.ShouldBe("MsSqlConnection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetEffectiveTableNameStripsConfigurationBaseSuffix()
    {
        // Arrange
        var model = new ConfigurationModel
        {
            ClassName = "ConnectionConfigurationBase"
        };

        // Act
        var result = model.GetEffectiveTableName();

        // Assert
        result.ShouldBe("Connection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetEffectiveTableNameReturnsClassNameWhenNoSuffix()
    {
        // Arrange
        var model = new ConfigurationModel
        {
            ClassName = "CustomEntity"
        };

        // Act
        var result = model.GetEffectiveTableName();

        // Assert
        result.ShouldBe("CustomEntity");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetEffectiveDisplayNameReturnsExplicitDisplayName()
    {
        // Arrange
        var model = new ConfigurationModel
        {
            ClassName = "TestConfiguration",
            DisplayName = "Test Configuration"
        };

        // Act
        var result = model.GetEffectiveDisplayName();

        // Assert
        result.ShouldBe("Test Configuration");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetEffectiveDisplayNameReturnsClassNameWhenNotSpecified()
    {
        // Arrange
        var model = new ConfigurationModel
        {
            ClassName = "MsSqlConfiguration"
        };

        // Act
        var result = model.GetEffectiveDisplayName();

        // Assert
        result.ShouldBe("MsSqlConfiguration");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetParentForeignKeyColumnReturnsNullWhenNoParent()
    {
        // Arrange
        var model = new ConfigurationModel
        {
            ClassName = "RootConfiguration"
        };

        // Act
        var result = model.GetParentForeignKeyColumn();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetParentForeignKeyColumnReturnsExplicitForeignKeyColumn()
    {
        // Arrange
        var model = new ConfigurationModel
        {
            ParentTableName = "Parent",
            ExplicitParentForeignKeyColumn = "CustomParentId"
        };

        // Act
        var result = model.GetParentForeignKeyColumn();

        // Assert
        result.ShouldBe("CustomParentId");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetParentForeignKeyColumnReturnsDefaultForeignKeyColumn()
    {
        // Arrange
        var model = new ConfigurationModel
        {
            ParentTableName = "Connection"
        };

        // Act
        var result = model.GetParentForeignKeyColumn();

        // Assert
        result.ShouldBe("ConnectionId");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DefaultSchemaIsCfg()
    {
        // Arrange & Act
        var model = new ConfigurationModel();

        // Assert
        model.Schema.ShouldBe("cfg");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DefaultGenerateDdlIsTrue()
    {
        // Arrange & Act
        var model = new ConfigurationModel();

        // Assert
        model.GenerateDdl.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DefaultGenerateValidatorIsTrue()
    {
        // Arrange & Act
        var model = new ConfigurationModel();

        // Assert
        model.GenerateValidator.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DefaultGenerateUiIsTrue()
    {
        // Arrange & Act
        var model = new ConfigurationModel();

        // Assert
        model.GenerateUi.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DefaultOnDeleteIsCascade()
    {
        // Arrange & Act
        var model = new ConfigurationModel();

        // Assert
        model.OnDelete.ShouldBe("Cascade");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DefaultDatabaseProviderIsMsSql()
    {
        // Arrange & Act
        var model = new ConfigurationModel();

        // Assert
        model.DatabaseProvider.ShouldBe("MsSql");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void PropertiesInitializesToEmptyList()
    {
        // Arrange & Act
        var model = new ConfigurationModel();

        // Assert
        model.Properties.ShouldNotBeNull();
        model.Properties.ShouldBeEmpty();
    }

}
