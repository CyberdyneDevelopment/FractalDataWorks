using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Data.Builders.Results;
using Fdw.Results;
using Shouldly;
using Xunit;

namespace Fdw.Data.Builders.Tests;

/// <summary>
/// Tests for the BuilderResultCodes TypeCollection.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class BuilderResultCodesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllReturnsAllResultCodes()
    {
        // Act
        var all = BuilderResultCodes.All();

        // Assert
        all.ShouldNotBeNull();
        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsCorrectResultCode()
    {
        // Arrange
        var expected = BuilderResultCodes.ByName("StoreIdRequired");

        // Act
        var actual = BuilderResultCodes.ById(expected.Id);

        // Assert
        actual.ShouldNotBeNull();
        actual.Id.ShouldBe(expected.Id);
        actual.Name.ShouldBe("StoreIdRequired");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsCorrectResultCode()
    {
        // Act
        var result = BuilderResultCodes.ByName("StoreIdRequired");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("StoreIdRequired");
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        result.Code.ShouldBe($"BUILDER-{result.Id}");
        result.EventId.ShouldBe(result.Id);
        result.Domain.ShouldBe("BUILDER");
        result.MessageTemplate.ShouldBe("Store ID is required");
        result.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsNotFoundForInvalidName()
    {
        // Act
        var result = BuilderResultCodes.ByName("NonExistentCode");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("NotFound");
        result.Id.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsNotFoundForInvalidId()
    {
        // Act
        var result = BuilderResultCodes.ById(999999);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("NotFound");
        result.Id.ShouldBe(0);
        result.Code.ShouldBe("UNKNOWN");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllResultCodesHaveUniqueIds()
    {
        // Act
        var all = BuilderResultCodes.All();
        var ids = all.Select(rc => rc.Id).ToList();

        // Assert
        ids.ShouldBeUnique();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllResultCodesHaveUniqueNames()
    {
        // Act
        var all = BuilderResultCodes.All();
        var names = all.Select(rc => rc.Name).ToList();

        // Assert
        names.ShouldBeUnique();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllResultCodesHaveUniqueEventIds()
    {
        // Act
        var all = BuilderResultCodes.All();
        var eventIds = all.Select(rc => rc.EventId).ToList();

        // Assert
        eventIds.ShouldBeUnique();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllResultCodesFollowCatalogInvariants()
    {
        // Act
        var all = BuilderResultCodes.All();

        // Assert
        // Codes are categorized numbers (resultcode-catalog): Code == "BUILDER-{number}",
        // Id == EventId == number, Domain == "BUILDER". Assert the invariants rather than
        // hardcoding the (renumber-prone) per-code numbers or an EventId range.
        foreach (var resultCode in all)
        {
            if (string.Equals(resultCode.Name, "NotFound", System.StringComparison.Ordinal))
            {
                continue;
            }

            resultCode.Code.ShouldBe($"BUILDER-{resultCode.Id}");
            resultCode.EventId.ShouldBe(resultCode.Id);
            resultCode.Domain.ShouldBe("BUILDER");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllResultCodesHaveBuilderDomain()
    {
        // Act
        var all = BuilderResultCodes.All();

        // Assert
        // Catalog model: Domain == the categorized prefix "BUILDER" (the NotFound sentinel is "Unknown").
        foreach (var resultCode in all)
        {
            if (string.Equals(resultCode.Name, "NotFound", System.StringComparison.Ordinal))
            {
                continue;
            }

            resultCode.Domain.ShouldBe("BUILDER");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DatasetMissingFieldsFormatsMessageCorrectly()
    {
        // Arrange
        var code = BuilderResultCodes.ByName("DatasetMissingFields");
        var details = ResultDetails.Create().With("DatasetName", "TestDataset");

        // Act
        var message = code.FormatMessage(details);

        // Assert
        message.ShouldBe("Dataset 'TestDataset' must have at least one field");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DatasetInvalidKeyFieldsFormatsMessageCorrectly()
    {
        // Arrange
        var code = BuilderResultCodes.ByName("DatasetInvalidKeyFields");
        var details = ResultDetails.Create()
            .With("DatasetName", "TestDataset")
            .With("InvalidKeyFields", "Field1, Field2");

        // Act
        var message = code.FormatMessage(details);

        // Assert
        message.ShouldBe("Dataset 'TestDataset' has key fields that don't exist in the field list: Field1, Field2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParameterTypeMismatchFormatsMessageCorrectly()
    {
        // Arrange
        var code = BuilderResultCodes.ByName("ParameterTypeMismatch");
        var details = ResultDetails.Create()
            .With("ParameterName", "id")
            .With("ActualType", "String")
            .With("ExpectedType", "Int32");

        // Act
        var message = code.FormatMessage(details);

        // Assert
        message.ShouldBe("Parameter 'id' has type 'String' but expected 'Int32'");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("StoreIdRequired")]
    [InlineData("StoreNameRequired")]
    [InlineData("StoreTypeRequired")]
    [InlineData("TranslatorTypeRequired")]
    [InlineData("StoreLocationRequired")]
    [InlineData("StoreConfigurationRequired")]
    [InlineData("DatasetNameRequired")]
    [InlineData("RecordTypeNameRequired")]
    [InlineData("DatasetMissingFields")]
    [InlineData("DatasetMissingKeyFields")]
    [InlineData("DatasetDuplicateFields")]
    [InlineData("DatasetInvalidKeyFields")]
    [InlineData("FieldNameRequired")]
    [InlineData("FieldTypeRequired")]
    [InlineData("FieldInvalidMaxLength")]
    [InlineData("PathIdRequired")]
    [InlineData("ContainerTypeRequired")]
    [InlineData("PathNameRequired")]
    [InlineData("PathTypeRequired")]
    [InlineData("PathMissingSpecification")]
    [InlineData("ParameterNameRequired")]
    [InlineData("ParameterTypeRequired")]
    [InlineData("ParameterRequiredWithDefault")]
    [InlineData("ParameterDefaultTypeMismatch")]
    [InlineData("ParametersMissing")]
    [InlineData("ParameterMissing")]
    [InlineData("ParameterTypeMismatch")]
    public void AllDefinedResultCodesCanBeRetrievedByName(string name)
    {
        // Act
        var result = BuilderResultCodes.ByName(name);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(name);
        result.Id.ShouldBeGreaterThan(0);
    }
}
