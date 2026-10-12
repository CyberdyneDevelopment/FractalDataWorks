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
}
