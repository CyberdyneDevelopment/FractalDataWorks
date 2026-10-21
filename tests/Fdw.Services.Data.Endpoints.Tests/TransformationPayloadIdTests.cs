using System;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Data.Endpoints;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// Tests for the id a transformation's payload states (FDW-2007): the same id a mapping's chain stores for it, so the console can show a chain's names and
/// tell a deleted transformation from a known one.
/// </summary>
public sealed class TransformationPayloadIdTests
{
    private static readonly Guid TrimId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static FieldTransformationConfiguration Trim() => new()
    {
        Id = TrimId,
        Name = "Trim",
        Implementation = FieldTransformationConfiguration.ImplementationName,
        TransformationType = "Trim",
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AListedTransformationStatesTheIdAChainStoresForIt()
    {
        var payload = TransformationEndpointSupport.ToPayload(Trim());

        payload.Id.ShouldBe(TrimId);
        payload.Name.ShouldBe("Trim");
    }
}
