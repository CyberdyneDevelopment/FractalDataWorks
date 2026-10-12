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
