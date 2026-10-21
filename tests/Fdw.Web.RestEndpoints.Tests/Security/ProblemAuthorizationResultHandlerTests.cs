using System.IO;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Fdw.Web.RestEndpoints.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Fdw.Web.RestEndpoints.Tests.Security;

/// <summary>
/// Unit tests for <see cref="ProblemAuthorizationResultHandler"/> (FDW-2271): a refusal by the authorization middleware answers with the problem body and the request's reference.
/// </summary>
public sealed class ProblemAuthorizationResultHandlerTests
{
    private static DefaultHttpContext BuildContext(string? policyName)
    {
        // Why request services: writing a JSON result asks the request's services for its serializer options, and every real request has them.
        var httpContext = new DefaultHttpContext { RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider() };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "42")], "Bearer"));
        var metadata = policyName is null
            ? EndpointMetadataCollection.Empty
            : new EndpointMetadataCollection(new AuthorizeAttribute { Policy = policyName });
        httpContext.SetEndpoint(new Endpoint(null, metadata, "test"));
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static AuthorizationPolicy Policy() => new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authorization")]
    public async Task HandleAsyncForbiddenAnswers403WithTheRequestReferenceAndThePermissionNeeded()
    {
        var context = BuildContext("ui-feedback:write");
        var sut = new ProblemAuthorizationResultHandler(NullLogger<ProblemAuthorizationResultHandler>.Instance);
        var nextCalled = false;

        await sut.HandleAsync(_ => { nextCalled = true; return Task.CompletedTask; }, context, Policy(), PolicyAuthorizationResult.Forbid());

        nextCalled.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(403);
        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
        body.RootElement.GetProperty("referenceId").GetString().ShouldBe(context.TraceIdentifier);
        body.RootElement.GetProperty("detail").GetString().ShouldContain("ui-feedback:write");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authorization")]
    public async Task HandleAsyncSucceededContinuesToTheEndpoint()
    {
        var context = BuildContext("ui-feedback:write");
        var sut = new ProblemAuthorizationResultHandler(NullLogger<ProblemAuthorizationResultHandler>.Instance);
        var nextCalled = false;

        await sut.HandleAsync(_ => { nextCalled = true; return Task.CompletedTask; }, context, Policy(), PolicyAuthorizationResult.Success());

        nextCalled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(200);
    }
}
