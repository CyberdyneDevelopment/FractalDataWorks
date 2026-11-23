using System;
using Fdw.Services.Tags.Configuration;
using Fdw.Services.Tags.Endpoints;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tags.Tests;

/// <summary>
/// No route loses a tag's description when it moves to the descriptions table (FDW-6208): <c>GET /tags/{name}</c> and <c>GET /tags</c> map the tag the provider
/// read and filled, so the response carries the text the fill put on it.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class TagRouteDescriptionTests
{
    [Fact]
    [Trait("Priority", "P0")]
    public void TheGetAndListRoutesCarryTheDescriptionTheProviderFilled()
    {
        var filled = new TagImplementationConfiguration
        {
            Id = Guid.Parse("00000000-0000-0000-0000-0000000000a1"),
            Name = "urgent",
            OwnerKind = "Tenant",
            OwnerId = Guid.Parse("00000000-0000-0000-0000-0000000000e1"),
            Description = "Needs an answer today",
        };

        var response = TagResponseMapper.From(filled);

        response.Description.ShouldBe("Needs an answer today");
        response.Name.ShouldBe("urgent");
    }
}
