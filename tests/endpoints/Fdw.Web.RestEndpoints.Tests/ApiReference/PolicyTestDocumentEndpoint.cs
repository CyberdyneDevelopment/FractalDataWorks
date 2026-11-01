using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.RestEndpoints.ApiReference;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.Tests.ApiReference;

/// <summary>A reference document that names a page, exposing its admission check to the tests.</summary>
[ExcludeFromCodeCoverage]
internal sealed class PolicyTestDocumentEndpoint : ApiReferenceDocumentEndpointBase<EmptyRequest>
{
    private readonly string? _pageName;

    public PolicyTestDocumentEndpoint(string? pageName)
        : base(new Mock<ILogger<ApiReferenceDocumentEndpointBase<EmptyRequest>>>().Object, new Mock<ISystemRoleConfiguration>().Object)
    {
        _pageName = pageName;
    }

    protected override string DocumentName => "policy-test";

    protected override string? PageName => _pageName;

    public IGenericResult Check(ApiReferenceCaller caller, IConfiguration configuration) => Admit(caller, configuration);

    protected override Task<IGenericResult> Filter(
        ApiReferenceDocument document, ApiReferenceCaller caller, EmptyRequest request, CancellationToken cancellationToken) =>
        Task.FromResult<IGenericResult>(GenericResult.Success());
}
