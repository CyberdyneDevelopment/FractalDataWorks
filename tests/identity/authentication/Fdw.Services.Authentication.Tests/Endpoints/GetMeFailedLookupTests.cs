using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Models;
using Fdw.Services.Authentication.Endpoints;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Tests.Endpoints;

/// <summary>
/// GET /users/me answers a failed user lookup with that lookup's own message and code (FDW-6466), and a lookup that is a not found still
/// answers 404 by its own not-found code. Before, every failure was answered as "Me was not found".
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "Authentication")]
public sealed class GetMeFailedLookupTests
{
    private sealed class Probe(IGenericResult<GetMePayload> lookup) : GetMeEndpointBase<GetMePayload>(NullLogger.Instance)
    {
        protected override Task<IGenericResult<GetMePayload>> GetUserInfo(string username, CancellationToken ct) => Task.FromResult(lookup);
    }

    private static Probe Create(IGenericResult<GetMePayload> lookup)
        => Factory.Create<Probe>(
            ctx =>
            {
                ctx.Response.Body = new MemoryStream();
                ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "ada")], "test", ClaimTypes.Name, ClaimTypes.Role));
            },
            lookup);

    private static string Body(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return new StreamReader(context.Response.Body).ReadToEnd();
    }

    [Fact]
    public async Task ALookupThatFailedIsAnsweredWithItsOwnCodeNotAsMeNotFound()
    {
        var failed = GenericResult<GetMePayload>.Failure(new GenericMessage(MessageSeverity.Error, "SQL permission denied on SELECT of auth.User", "MSSQL-70229"));
        var endpoint = Create(failed);

        await endpoint.HandleAsync(TestContext.Current.CancellationToken);

        var (expectedStatus, _) = ResultHttpStatusMapper.Map(failed, new DefaultHttpContext());
        endpoint.HttpContext.Response.StatusCode.ShouldBe(expectedStatus);
        Body(endpoint.HttpContext).ShouldContain("MSSQL-70229");
    }

    [Fact]
    public async Task ALookupThatIsANotFoundIsStillAnswered404ByItsOwnCode()
    {
        var notFound = GenericResult<GetMePayload>.Failure(new GenericMessage(MessageSeverity.Warning, "the user was not found", "AUTH-30001"));
        var endpoint = Create(notFound);

        await endpoint.HandleAsync(TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
        Body(endpoint.HttpContext).ShouldContain("AUTH-30001");
    }
}
