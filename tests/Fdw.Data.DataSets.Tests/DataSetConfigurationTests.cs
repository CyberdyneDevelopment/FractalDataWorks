using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Configuration;
using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

public class DataSetConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new DataSetConfiguration();

        // Assert
        config.Name.ShouldBe(string.Empty);
        config.Description.ShouldBe(string.Empty);
        config.Version.ShouldBe("1.0");
        config.Category.ShouldBe("Dataset");
        config.RecordTypeName.ShouldBe(string.Empty);
        config.Fields.ShouldNotBeNull();
        config.Fields.ShouldBeEmpty();
        config.KeyFields.ShouldNotBeNull();
        config.KeyFields.ShouldBeEmpty();
        config.SourceIds.ShouldNotBeNull();
        config.SourceIds.ShouldBeEmpty();
        config.Caching.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Name_CanBeSet()
    {
        // Arrange
        var config = new DataSetConfiguration { Name = "TestDataSet" };

        // Assert
        config.Name.ShouldBe("TestDataSet");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Description_CanBeSet()
    {
        // Arrange
        var config = new DataSetConfiguration { Description = "Test Description" };

        // Assert
        config.Description.ShouldBe("Test Description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Version_CanBeSet()
    {
        // Arrange
        var config = new DataSetConfiguration { Version = "2.0" };

        // Assert
        config.Version.ShouldBe("2.0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Category_CanBeSet()
    {
        // Arrange
        var config = new DataSetConfiguration { Category = "Custom" };

        // Assert
        config.Category.ShouldBe("Custom");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void RecordTypeName_CanBeSet()
    {
        // Arrange
        var config = new DataSetConfiguration { RecordTypeName = "MyNamespace.MyType" };

        // Assert
        config.RecordTypeName.ShouldBe("MyNamespace.MyType");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Fields_CanBePopulated()
    {
        // Arrange
        var field = new DataFieldConfiguration { Name = "Id", TypeName = "System.Int32" };
        var config = new DataSetConfiguration();

        // Act
        config.Fields.Add(field);

        // Assert
        config.Fields.Count.ShouldBe(1);
        config.Fields[0].Name.ShouldBe("Id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void KeyFields_CanBePopulated()
    {
        // Arrange
        var config = new DataSetConfiguration();

        // Act
        config.KeyFields.Add(new DataSetKeyFieldConfiguration { KeyName = "Id", KeyType = "Surrogate", Ordinal = 0 });
        config.KeyFields.Add(new DataSetKeyFieldConfiguration { KeyName = "Tenant", KeyType = "Natural", Ordinal = 0 });

        // Assert
        config.KeyFields.Count.ShouldBe(2);
        config.KeyFields.ShouldContain(kf => kf.KeyName == "Id");
        config.KeyFields.ShouldContain(kf => kf.KeyName == "Tenant");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void SourceIds_CanBePopulated()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var config = new DataSetConfiguration();

        // Act
        config.Sources.Add(new DataSetSourceConfiguration { Id = sourceId });

        // Assert
        config.SourceIds.Count.ShouldBe(1);
        config.SourceIds[0].ShouldBe(sourceId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void SourceIds_CanContainMultipleEntries()
    {
        // Arrange
        var sourceId1 = Guid.NewGuid();
        var sourceId2 = Guid.NewGuid();
        var config = new DataSetConfiguration();

        // Act
        config.Sources.Add(new DataSetSourceConfiguration { Id = sourceId1 });
        config.Sources.Add(new DataSetSourceConfiguration { Id = sourceId2 });

        // Assert
        config.SourceIds.Count.ShouldBe(2);
        config.SourceIds.ShouldContain(sourceId1);
        config.SourceIds.ShouldContain(sourceId2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Aggregates_CanBePopulated()
    {
        // Arrange
        var config = new DataSetConfiguration();

        // Act
        config.Aggregates.Add(new DataSetAggregateDefinition
        {
            AggregateColumnName = "TotalSales",
            GroupByFieldNames = "State",
            AggregateFunctionName = "SUM",
            InputFieldName = "Amount",
            Ordinal = 0
        });

        // Assert
        config.Aggregates.Count.ShouldBe(1);
        config.Aggregates[0].AggregateColumnName.ShouldBe("TotalSales");
        config.Aggregates[0].AggregateFunctionName.ShouldBe("SUM");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Aggregates_ImplementIGenericConfigurationForCascade()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var aggregateId = Guid.NewGuid();

        // Act
        IGenericConfiguration aggregate = new DataSetAggregateDefinition
        {
            Id = aggregateId,
            DataSetId = dataSetId,
            AggregateColumnName = "TransactionCount",
            GroupByFieldNames = "State,Region",
            AggregateFunctionName = "COUNT",
            InputFieldName = "Id"
        };

        // Assert
        aggregate.Id.ShouldBe(aggregateId);
        aggregate.ServiceType.ShouldBe("DataSet");
        ((DataSetAggregateDefinition)aggregate).DataSetId.ShouldBe(dataSetId);
        ((DataSetAggregateDefinition)aggregate).IsCurrent.ShouldBeTrue();
        ((DataSetAggregateDefinition)aggregate).IsDeleted.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Caching_CanBeSet()
    {
        // Arrange
        var cachingConfig = new CachingConfiguration { Enabled = true, DurationMinutes = 30 };
        var config = new DataSetConfiguration { Caching = cachingConfig };

        // Assert
        config.Caching.ShouldNotBeNull();
        config.Caching.Enabled.ShouldBeTrue();
        config.Caching.DurationMinutes.ShouldBe(30);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void SectionName_HasCorrectStaticValue()
    {
        // Arrange
        var config = new DataSetConfiguration { Name = "Users" };

        // Act & Assert - SectionName is static for IOptions binding, Name is set separately
        config.SectionName.ShouldBe("DataSets");
        config.Name.ShouldBe("Users");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange
        var config = new DataSetConfiguration
        {
            Name = "Users",
            Description = "User dataset",
            Version = "2.0",
            Category = "Security",
            RecordTypeName = "My.Namespace.User",
            Caching = new CachingConfiguration { Enabled = true }
        };

        config.Fields.Add(new DataFieldConfiguration { Name = "Id", TypeName = "System.Int32" });
        config.KeyFields.Add(new DataSetKeyFieldConfiguration { KeyName = "Id", KeyType = "Surrogate", Ordinal = 0 });
        config.Sources.Add(new DataSetSourceConfiguration { Id = Guid.NewGuid() });

        // Assert
        config.Name.ShouldBe("Users");
        config.Description.ShouldBe("User dataset");
        config.Version.ShouldBe("2.0");
        config.Category.ShouldBe("Security");
        config.RecordTypeName.ShouldBe("My.Namespace.User");
        config.Fields.Count.ShouldBe(1);
        config.KeyFields.Count.ShouldBe(1);
        config.SourceIds.Count.ShouldBe(1);
        config.Caching.ShouldNotBeNull();
    }
}
