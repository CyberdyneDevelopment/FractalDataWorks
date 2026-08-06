using System;
using System.Linq;
using Fdw.Data.OData.Results;
using Fdw.Results.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.OData.Tests.Results;

/// <summary>
/// Tests for ODataResultCodes TypeCollection.
/// </summary>
public sealed class ODataResultCodesTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void AllReturnsAllResultCodes()
    {
        // Act
        var allCodes = ODataResultCodes.All();

        // Assert
        allCodes.ShouldNotBeNull();
        allCodes.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsCorrectResultCodeForContainerNull()
    {
        // Act
        var code = ODataResultCodes.ByName("ContainerNull");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("ContainerNull");
        // Catalog invariant: Code == "REST-{number}", Id == EventId == number, Domain == "REST".
        code.Code.ShouldBe($"REST-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("REST");
        code.Severity.Name.ShouldBe("Error");
        code.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsCorrectResultCodeForDeleteFilterRequired()
    {
        // Act
        var code = ODataResultCodes.ByName("DeleteFilterRequired");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("DeleteFilterRequired");
        code.Code.ShouldBe($"REST-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("REST");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsCorrectResultCodeForDeleteFilterInvalid()
    {
        // Act
        var code = ODataResultCodes.ByName("DeleteFilterInvalid");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("DeleteFilterInvalid");
        code.Code.ShouldBe($"REST-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("REST");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsCorrectResultCodeForDeleteResourceIdNotFound()
    {
        // Act
        var code = ODataResultCodes.ByName("DeleteResourceIdNotFound");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("DeleteResourceIdNotFound");
        code.Code.ShouldBe($"REST-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("REST");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsCorrectResultCodeForDeleteTranslationFailed()
    {
        // Act
        var code = ODataResultCodes.ByName("DeleteTranslationFailed");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("DeleteTranslationFailed");
        code.Code.ShouldBe($"REST-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("REST");
        code.MessageTemplate.ShouldContain("{ErrorMessage}");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsCorrectResultCodeForInsertDataRequired()
    {
        // Act
        var code = ODataResultCodes.ByName("InsertDataRequired");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("InsertDataRequired");
        code.Code.ShouldBe($"REST-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("REST");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsCorrectResultCodeForInsertTranslationFailed()
    {
        // Act
        var code = ODataResultCodes.ByName("InsertTranslationFailed");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("InsertTranslationFailed");
        code.Code.ShouldBe($"REST-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("REST");
        code.MessageTemplate.ShouldContain("{ErrorMessage}");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsCorrectResultCodeForQueryTranslationFailed()
    {
        // Act
        var code = ODataResultCodes.ByName("QueryTranslationFailed");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("QueryTranslationFailed");
        code.Code.ShouldBe($"REST-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("REST");
        code.MessageTemplate.ShouldContain("{ErrorMessage}");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsCorrectResultCodeForUpdateDataRequired()
    {
        // Act
        var code = ODataResultCodes.ByName("UpdateDataRequired");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("UpdateDataRequired");
        code.Code.ShouldBe($"REST-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("REST");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsCorrectResultCodeForUpdateResourceIdNotFound()
    {
        // Act
        var code = ODataResultCodes.ByName("UpdateResourceIdNotFound");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("UpdateResourceIdNotFound");
        code.Code.ShouldBe($"REST-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("REST");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsCorrectResultCodeForUpdateTranslationFailed()
    {
        // Act
        var code = ODataResultCodes.ByName("UpdateTranslationFailed");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("UpdateTranslationFailed");
        code.Code.ShouldBe($"REST-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("REST");
        code.MessageTemplate.ShouldContain("{ErrorMessage}");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsEmptyForInvalidName()
    {
        // Act
        var code = ODataResultCodes.ByName("InvalidName");

        // Assert
        code.ShouldNotBeNull();
        code.Id.ShouldBe(0); // Empty/NotFound has Id 0
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsCorrectResultCode()
    {
        // Arrange: resolve the catalog number for a known code rather than hardcoding it,
        // so a future renumber doesn't break this test.
        var expected = ODataResultCodes.ByName("ContainerNull");

        // Act
        var code = ODataResultCodes.ById(expected.Id);

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("ContainerNull");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsEmptyForInvalidId()
    {
        // Act
        var code = ODataResultCodes.ById(9999999);

        // Assert
        code.ShouldNotBeNull();
        code.Id.ShouldBe(0); // Empty/NotFound has Id 0
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void AllResultCodesFollowCatalogInvariants()
    {
        // Codes are categorized numbers (resultcode-catalog): Code == "REST-{number}",
        // Id == EventId == number, Domain == "REST". Assert the invariants rather than
        // hardcoding the (renumber-prone) per-code numbers or a per-domain EventId range.
        foreach (var code in ODataResultCodes.All())
        {
            if (string.Equals(code.Name, "NotFound", StringComparison.Ordinal))
            {
                continue;
            }

            code.Code.ShouldBe($"REST-{code.Id}");
            code.EventId.ShouldBe(code.Id);
            code.Domain.ShouldBe("REST", $"Code {code.Name} has incorrect domain");
        }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void AllEventIdsAreUnique()
    {
        // Act
        var allCodes = ODataResultCodes.All();
        var eventIds = allCodes.Select(c => c.EventId).ToList();

        // Assert
        eventIds.Distinct().Count().ShouldBe(eventIds.Count, "Event IDs must be unique");
    }
}
