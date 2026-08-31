using System;
using Fdw.Data.Builders;
using Shouldly;
using Xunit;

namespace Fdw.Data.Builders.Tests;

public sealed class DataFieldConfigurationBuilderTests
{
    private static DataFieldConfigurationBuilder CreateValidBuilder() =>
        new DataFieldConfigurationBuilder()
            .WithName("CustomerId")
            .WithType<int>();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildSucceedsWithNameAndType()
    {
        var result = CreateValidBuilder().Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("CustomerId");
        result.Value.TypeName.ShouldBe(typeof(int).FullName);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildFailsWithoutName()
    {
        var result = new DataFieldConfigurationBuilder()
            .WithType<int>()
            .Build();

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildFailsWithWhitespaceName()
    {
        var result = new DataFieldConfigurationBuilder()
            .WithName("  ")
            .WithType<int>()
            .Build();

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildFailsWithoutType()
    {
        var result = new DataFieldConfigurationBuilder()
            .WithName("CustomerId")
            .Build();

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithTypeGenericSetsTypeName()
    {
        var result = new DataFieldConfigurationBuilder()
            .WithName("Name")
            .WithType<string>()
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.TypeName.ShouldBe(typeof(string).FullName);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithTypeInstanceSetsTypeName()
    {
        var result = new DataFieldConfigurationBuilder()
            .WithName("Name")
            .WithType(typeof(decimal))
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.TypeName.ShouldBe(typeof(decimal).FullName);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithTypeInstanceThrowsForNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DataFieldConfigurationBuilder().WithType(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithTypeNameSetsTypeName()
    {
        var result = new DataFieldConfigurationBuilder()
            .WithName("Name")
            .WithTypeName("System.String")
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.TypeName.ShouldBe("System.String");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithDescriptionSetsDescription()
    {
        var result = CreateValidBuilder()
            .WithDescription("Primary key field")
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Description.ShouldBe("Primary key field");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithDescriptionNullSetsNull()
    {
        var result = CreateValidBuilder()
            .WithDescription(null)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AsKeySetsIsKeyAndIsRequired()
    {
        var result = CreateValidBuilder()
            .AsKey()
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.IsKey.ShouldBeTrue();
        result.Value.IsRequired.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AsRequiredSetsIsRequired()
    {
        var result = CreateValidBuilder()
            .AsRequired()
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.IsRequired.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AsOptionalUnsetsIsRequired()
    {
        var result = CreateValidBuilder()
            .AsRequired()
            .AsOptional()
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.IsRequired.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AsOptionalDoesNotUnsetIsRequiredWhenKey()
    {
        var result = CreateValidBuilder()
            .AsKey()
            .AsOptional()
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.IsKey.ShouldBeTrue();
        result.Value.IsRequired.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AsIndexedSetsIsIndexed()
    {
        var result = CreateValidBuilder()
            .AsIndexed()
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.IsIndexed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithMaxLengthSetsMaxLength()
    {
        var result = CreateValidBuilder()
            .WithMaxLength(100)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.MaxLength.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildFailsWithZeroMaxLength()
    {
        var result = CreateValidBuilder()
            .WithMaxLength(0)
            .Build();

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildFailsWithNegativeMaxLength()
    {
        var result = CreateValidBuilder()
            .WithMaxLength(-1)
            .Build();

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithDefaultValueStringSetsDefaultValue()
    {
        var result = CreateValidBuilder()
            .WithDefaultValue("default")
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.DefaultValue.ShouldBe("default");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithDefaultValueObjectSetsDefaultValue()
    {
        var result = CreateValidBuilder()
            .WithDefaultValue((object)42)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.DefaultValue.ShouldBe("42");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithDefaultValueNullObjectSetsNull()
    {
        var result = CreateValidBuilder()
            .WithDefaultValue((object?)null)
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.DefaultValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ResetClearsAllValues()
    {
        var builder = CreateValidBuilder()
            .WithDescription("desc")
            .AsKey()
            .AsIndexed()
            .WithMaxLength(50)
            .WithDefaultValue("test");

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
            .WithName("OrderId")
            .WithType<Guid>()
            .Build();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("OrderId");
        result.Value.TypeName.ShouldBe(typeof(Guid).FullName);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildSetsAllConfiguredProperties()
    {
        var result = new DataFieldConfigurationBuilder()
            .WithName("Amount")
            .WithType<decimal>()
            .WithDescription("Transaction amount")
            .AsRequired()
            .AsIndexed()
            .WithMaxLength(18)
            .WithDefaultValue("0")
            .Build();

        result.IsSuccess.ShouldBeTrue();
        var field = result.Value!;
        field.Name.ShouldBe("Amount");
        field.TypeName.ShouldBe(typeof(decimal).FullName);
        field.Description.ShouldBe("Transaction amount");
        field.IsRequired.ShouldBeTrue();
        field.IsIndexed.ShouldBeTrue();
        field.MaxLength.ShouldBe(18);
        field.DefaultValue.ShouldBe("0");
    }
}
