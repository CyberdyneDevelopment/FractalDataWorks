using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Builders;
using Fdw.Data.DataContainers.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.Builders.Tests;

public sealed class DataSetConfigurationBuilderTests
{
    private static DataSetConfigurationBuilder CreateValidBuilder()
    {
        return new DataSetConfigurationBuilder()
            .WithName("Customers")
            .WithRecordType<TestRecord>()
            .AddField("Id", "System.Int32", isKey: true)
            .AddField("Name", "System.String");
    }

    private sealed class TestRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildSucceedsWithRequiredProperties()
    {
        var result = CreateValidBuilder().Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("Customers");
        result.Value.Fields.Count.ShouldBe(2);
        result.Value.KeyFields.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildFailsWithoutName()
    {
        var result = new DataSetConfigurationBuilder()
            .WithRecordType<TestRecord>()
            .AddField("Id", "System.Int32", isKey: true)
            .Build();

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildFailsWithWhitespaceName()
    {
        var result = new DataSetConfigurationBuilder()
            .WithName("  ")
            .WithRecordType<TestRecord>()
            .AddField("Id", "System.Int32", isKey: true)
            .Build();

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildFailsWithoutRecordType()
    {
        var result = new DataSetConfigurationBuilder()
            .WithName("Customers")
            .AddField("Id", "System.Int32", isKey: true)
            .Build();

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildFailsWithoutFields()
    {
        var result = new DataSetConfigurationBuilder()
            .WithName("Customers")
            .WithRecordType<TestRecord>()
            .AddKeyField("Id")
            .Build();

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildFailsWithoutKeyFields()
    {
        var result = new DataSetConfigurationBuilder()
            .WithName("Customers")
            .WithRecordType<TestRecord>()
            .AddField("Name", "System.String")
            .Build();

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildFailsWithDuplicateFieldNames()
    {
        var result = new DataSetConfigurationBuilder()
            .WithName("Customers")
            .WithRecordType<TestRecord>()
            .AddField("Id", "System.Int32", isKey: true)
            .AddField("Id", "System.String")
            .Build();

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildFailsWithInvalidKeyFields()
    {
        var result = new DataSetConfigurationBuilder()
            .WithName("Customers")
            .WithRecordType<TestRecord>()
            .AddField("Id", "System.Int32")
            .AddKeyField("NonExistent")
            .Build();

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithIdSetsCustomId()
    {
        var id = Guid.NewGuid();
        var result = CreateValidBuilder()
            .WithId(id)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithDescriptionSetsDescription()
    {
        var result = CreateValidBuilder()
            .WithDescription("Customer records")
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Description.ShouldBe("Customer records");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithVersionSetsVersion()
    {
        var result = CreateValidBuilder()
            .WithVersion("2.0")
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Version.ShouldBe("2.0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithVersionNullDefaultsTo10()
    {
        var result = CreateValidBuilder()
            .WithVersion(null!)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Version.ShouldBe("1.0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithCategorySetsCategory()
    {
        var result = CreateValidBuilder()
            .WithCategory("Reference")
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Category.ShouldBe("Reference");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithCategoryNullDefaultsToDataset()
    {
        var result = CreateValidBuilder()
            .WithCategory(null!)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Category.ShouldBe("Dataset");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithRecordTypeGenericSetsRecordTypeName()
    {
        var result = CreateValidBuilder().Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RecordTypeName.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithRecordTypeInstanceSetsRecordTypeName()
    {
        var result = new DataSetConfigurationBuilder()
            .WithName("Customers")
            .WithRecordType(typeof(TestRecord))
            .AddField("Id", "System.Int32", isKey: true)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RecordTypeName.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithRecordTypeInstanceThrowsForNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DataSetConfigurationBuilder().WithRecordType((Type)null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithRecordTypeNameSetsTypeName()
    {
        var result = new DataSetConfigurationBuilder()
            .WithName("Customers")
            .WithRecordTypeName("MyApp.Customer")
            .AddField("Id", "System.Int32", isKey: true)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RecordTypeName.ShouldBe("MyApp.Customer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddFieldWithConfigurationAddsField()
    {
        var field = new DataFieldConfiguration
        {
            Name = "Email",
            TypeName = "System.String",
            IsRequired = true
        };

        var result = CreateValidBuilder()
            .AddField(field)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Fields.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddFieldNullIsIgnored()
    {
        var result = CreateValidBuilder()
            .AddField((DataFieldConfiguration)null!)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Fields.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddFieldWithKeyAutoAddsToKeyFields()
    {
        var field = new DataFieldConfiguration
        {
            Name = "CompanyId",
            TypeName = "System.Int32",
            IsKey = true
        };

        var result = CreateValidBuilder()
            .AddField(field)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.KeyFields.ShouldContain(kf => kf.KeyName == "CompanyId");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddFieldWithBuilderFuncAddsField()
    {
        var result = CreateValidBuilder()
            .AddField(b => b.WithName("Email").WithType<string>().Build())
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Fields.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddFieldWithBuilderFuncNullIsIgnored()
    {
        var result = CreateValidBuilder()
            .AddField((Func<DataFieldConfigurationBuilder, IGenericResult<DataFieldConfiguration>>)null!)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Fields.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddFieldsAddsMultipleFields()
    {
        var fields = new[]
        {
            new DataFieldConfiguration { Name = "Email", TypeName = "System.String" },
            new DataFieldConfiguration { Name = "Phone", TypeName = "System.String" }
        };

        var result = CreateValidBuilder()
            .AddFields(fields)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Fields.Count.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddFieldsNullIsIgnored()
    {
        var result = CreateValidBuilder()
            .AddFields(null!)
            .Build();

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithCalculatedFieldAddsField()
    {
        var result = CreateValidBuilder()
            .WithCalculatedField("FullName", typeof(string), row => "test")
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Fields.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithCalculatedFieldGenericAddsField()
    {
        var result = CreateValidBuilder()
            .WithCalculatedField<string>("FullName", row => "test", "Computed full name")
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Fields.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddKeyFieldAddsToKeyFields()
    {
        var result = CreateValidBuilder()
            .AddField("CompanyId", "System.Int32")
            .AddKeyField("CompanyId")
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.KeyFields.ShouldContain(kf => kf.KeyName == "CompanyId");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddKeyFieldIgnoresEmptyString()
    {
        var result = CreateValidBuilder()
            .AddKeyField("")
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.KeyFields.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddKeyFieldIgnoresDuplicates()
    {
        var result = CreateValidBuilder()
            .AddKeyField("Id")
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.KeyFields.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddKeyFieldsAddsMultiple()
    {
        var result = new DataSetConfigurationBuilder()
            .WithName("Composite")
            .WithRecordType<TestRecord>()
            .AddField("Id", "System.Int32")
            .AddField("CompanyId", "System.Int32")
            .AddKeyFields(new[] { "Id", "CompanyId" })
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.KeyFields.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddKeyFieldsNullIsIgnored()
    {
        var result = CreateValidBuilder()
            .AddKeyFields(null!)
            .Build();

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddSourceAddsSourceConfiguration()
    {
        var result = CreateValidBuilder()
            .AddSource("Primary", "MsSql")
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.SourceIds.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddSourceReplacesExistingWithSameName()
    {
        var result = CreateValidBuilder()
            .AddSource("Primary", "MsSql", priority: 100)
            .AddSource("Primary", "PostgreSql", priority: 50)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.SourceIds.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddSourceIgnoresEmptySourceName()
    {
        var result = CreateValidBuilder()
            .AddSource("", "MsSql")
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.SourceIds.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddSourceConfigurationSetsDataSetId()
    {
        var id = Guid.NewGuid();
        var builder = CreateValidBuilder()
            .WithId(id)
            .AddSource("Primary", "MsSql");

        var result = builder.Build();
        result.IsSuccess.ShouldBeTrue();

        builder.SourceConfigurations.ShouldAllBe(s => s.DataSetId == id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddSourcesAddsMultipleSources()
    {
        var sources = new[]
        {
            new DataSetSourceConfiguration { SourceName = "Primary", ConnectionType = "MsSql" },
            new DataSetSourceConfiguration { SourceName = "Fallback", ConnectionType = "Http" }
        };

        var result = CreateValidBuilder()
            .AddSources(sources)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.SourceIds.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AddSourcesNullIsIgnored()
    {
        var result = CreateValidBuilder()
            .AddSources(null!)
            .Build();

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithCachingSetsCaching()
    {
        var caching = new CachingConfiguration();
        var result = CreateValidBuilder()
            .WithCaching(caching)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Caching.ShouldBe(caching);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ResetClearsAllValues()
    {
        var builder = CreateValidBuilder()
            .WithDescription("test")
            .AddSource("Primary", "MsSql");

        builder.Reset();

        var result = builder.Build();
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ResetAllowsRebuildWithNewValues()
    {
        var builder = CreateValidBuilder();
        builder.Reset();

        var result = builder
            .WithName("Orders")
            .WithRecordType<TestRecord>()
            .AddField("OrderId", "System.Guid", isKey: true)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("Orders");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildSetsDefaultValues()
    {
        var result = CreateValidBuilder().Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Version.ShouldBe("1.0");
        result.Value.Category.ShouldBe("Dataset");
        result.Value.Id.ShouldNotBe(Guid.Empty);
    }
}
