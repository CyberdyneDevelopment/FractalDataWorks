using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.RestEndpoints.Tests.Crud;

/// <summary>
/// FDW-978: an endpoint that reads one record chooses between GET, naming the record in the route, and
/// POST, naming it in the body. GET is a plain paged list and nothing else, so a converted endpoint opts in.
/// </summary>
/// <remarks>
/// The route is what an endpoint registers; the verb follows the same switch in <c>Configure</c>. A test
/// subclass exposes both, without a running host.
/// </remarks>
[Trait("Priority", "P1")]
[Trait("Category", "Endpoints")]
public class CrudGetEndpointBaseTests
{
    [Fact]
    public void AnEndpointThatHasNotOptedInReadsByGetWithTheNameInTheRoute()
    {
        var endpoint = new WidgetByRouteEndpoint();

        endpoint.ReadsByBody.ShouldBeFalse();
        endpoint.RegisteredRoute.ShouldBe("/widgets/{Name}");
    }

    [Fact]
    public void AnEndpointThatOptsInReadsByPostAtTheGetRouteOfItsResource()
    {
        var endpoint = new WidgetByBodyEndpoint();

        endpoint.ReadsByBody.ShouldBeTrue();
        endpoint.RegisteredRoute.ShouldBe("/widgets/get");
    }

    private sealed class WidgetRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class WidgetDetail
    {
    }

    private class WidgetByRouteEndpoint : CrudGetEndpointBase<WidgetRequest, WidgetDetail>
    {
        public WidgetByRouteEndpoint() : base(NullLogger.Instance)
        {
        }

        public string RegisteredRoute => Route;

        public bool ReadsByBody => ReadByBody;

        protected override string ResourceName => "widgets";

        protected override string GetResourceIdentifier(WidgetRequest request) => request.Name;

        protected override Task<IGenericResult<WidgetDetail?>> FindByIdentifier(WidgetRequest request, CancellationToken ct)
            => Task.FromResult(GenericResult<WidgetDetail?>.Success(null));
    }

    private sealed class WidgetByBodyEndpoint : WidgetByRouteEndpoint
    {
        protected override bool ReadByBody => true;
    }
}
