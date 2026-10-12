using Fdw.Configuration;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests.EndpointBehavior;

/// <summary>
/// FDW-1194: the connection lookup offers exactly the rows the connection list offers, and sits under the connections
/// resource it is gated by.
/// </summary>
public sealed class LookupConnectionsEndpointBaseTests
{
    [Fact]
    public void ItIsRoutedUnderTheConnectionsResource()
    {
        var endpoint = new ConnectionLookup();

        endpoint.RegisteredRoute.ShouldBe("/connections/lookup");
        endpoint.RegisteredPolicy.ShouldBe("connections:read");
    }

    [Theory]
    [InlineData("Orders", "MsSql", true)]
    [InlineData("", "MsSql", false)]
    [InlineData("Orders", "", false)]
    [InlineData("Orders", null, false)]
    [InlineData("Orders", "Connection", false)]
    [InlineData("Orders", "connection", false)]
    public void ARowIsOfferedWhenItHasANameAndAKindOtherThanTheDomain(string name, string? implementation, bool offered)
    {
        var row = new DomainConfiguration { Name = name, Implementation = implementation };

        new ConnectionLookup().Offers(row).ShouldBe(offered);
    }

    private sealed class ConnectionLookup : LookupConnectionsEndpointBase
    {
        public ConnectionLookup()
            : base(NullLogger<LookupConnectionsEndpointBase>.Instance, null!)
        {
        }

        public string RegisteredRoute => Route;

        public string RegisteredPolicy => ReadPolicy;

        public bool Offers(DomainConfiguration row) => Includes(row);
    }
}
