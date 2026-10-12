using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.ApiReference;
using Microsoft.AspNetCore.Http;

namespace Fdw.Web.RestEndpoints.Tests.ApiReference;

/// <summary>A signed-in reference page that declares one policy, for the page-policy tests.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ApiReferencePages), "PolicyTestPage")]
public sealed class PolicyTestPage : ApiReferencePageBase
{
    public PolicyTestPage()
        : base("PolicyTestPage", "/test/policy", "Policy test", true, [PolicyTestPages.DeclaredPolicy], string.Empty)
    {
    }

    public override string DocumentUrl(HttpContext context) => "/openapi/policy-test";
}
