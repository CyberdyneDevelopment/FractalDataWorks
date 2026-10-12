using Fdw.Configuration;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// FDW-1194: the pipeline lookup offers exactly the rows the pipeline list offers, and sits under the pipelines resource it
/// is gated by.
/// </summary>
public sealed class LookupPipelinesEndpointBaseTests
{
    [Fact]
    public void ItIsRoutedUnderThePipelinesResource()
    {
        var endpoint = new PipelineLookup();

        endpoint.RegisteredRoute.ShouldBe("/pipelines/lookup");
        endpoint.RegisteredPolicy.ShouldBe("pipelines:read");
    }

    [Theory]
    [InlineData("BreweriesIngest", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void ARowIsOfferedWhenItHasAName(string name, bool offered)
    {
        new PipelineLookup().Offers(new DomainConfiguration { Name = name }).ShouldBe(offered);
    }

    private sealed class PipelineLookup : LookupPipelinesEndpointBase
    {
        public PipelineLookup()
            : base(NullLogger<LookupPipelinesEndpointBase>.Instance, null!)
        {
        }

        public string RegisteredRoute => Route;

        public string RegisteredPolicy => ReadPolicy;

        public bool Offers(DomainConfiguration row) => Includes(row);
    }
}
