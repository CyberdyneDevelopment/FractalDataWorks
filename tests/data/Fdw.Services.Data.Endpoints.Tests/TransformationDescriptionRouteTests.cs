using Fdw.Data.Transformations.Configuration;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6208: a transformation's description is no longer a column of its kind's table. The transformation routes (<c>GET /transformations</c>, <c>GET /transformations/{name}</c> and the
/// create and update answers) still carry it: the provider fills the member from the descriptions table on every read, and the payload mapping reads the member as it always did.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "Api")]
public sealed class TransformationDescriptionRouteTests
{
    [Fact]
    public void AFilterTransformationsPayloadCarriesTheDescriptionTheProviderFilled()
    {
        var filter = new FilterTransformationConfiguration
        {
            Id = System.Guid.NewGuid(),
            Name = "active",
            Implementation = FilterTransformationConfiguration.ImplementationName,
            Description = "Keeps the active rows",
        };

        TransformationEndpointSupport.ToPayload(filter).Description.ShouldBe("Keeps the active rows");
    }

    [Fact]
    public void AMapTransformationsPayloadCarriesTheDescriptionTheProviderFilled()
    {
        var map = new MapTransformationConfiguration
        {
            Id = System.Guid.NewGuid(),
            Name = "rename",
            Implementation = MapTransformationConfiguration.ImplementationName,
            Description = "Renames the order columns",
        };

        TransformationEndpointSupport.ToPayload(map).Description.ShouldBe("Renames the order columns");
    }

    [Fact]
    public void ATransformationNobodyDescribedAnswersNoDescription()
    {
        var filter = new FilterTransformationConfiguration
        {
            Id = System.Guid.NewGuid(),
            Name = "active",
            Implementation = FilterTransformationConfiguration.ImplementationName,
        };

        TransformationEndpointSupport.ToPayload(filter).Description.ShouldBeNull();
    }
}
