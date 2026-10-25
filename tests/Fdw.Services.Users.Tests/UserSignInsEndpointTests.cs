using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.SignIns;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Endpoints;
using Fdw.Services.Users.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Users.Tests;

/// <summary>
/// FDW-2729: an account's sign-ins are read a page at a time, newest first, by the account itself or by someone who administers it, and by
/// nobody else. A page that cannot be read is a failure and not an empty list.
/// </summary>
[Trait("Category", "Security")]
public class UserSignInsEndpointTests
{
    private static readonly Guid Person = new("7e7e7e7e-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset At = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    private static ConfigurationStore<UserConfigurationProvider, IUserImplementationConfiguration, UserDomainConfiguration> Store()
        => ConfigurationStore.Users(new UserImplementationConfiguration
        {
            Id = Person,
            Username = "dana",
            IsActive = true,
            IsCurrent = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });

    private static List<SignInRecord> Records(int count)
        => [.. Enumerable.Range(0, count).Select(i => new SignInRecord
        {
            SignedInAt = At.AddMinutes(-i),
            ServiceName = "Console",
            FlowName = "password-session",
            IpAddress = "203.0.113.9",
            UserAgent = "Mozilla/5.0 Firefox/132.0",
            Browser = "Firefox",
            OperatingSystem = "Linux",
        })];

    private static Action<DefaultHttpContext> Caller(string subject, bool platformAdmin)
        => ctx =>
        {
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject)], "test"));
            var roles = new Mock<ISystemRoleConfiguration>();
            roles.Setup(r => r.IsPlatformAdmin(It.IsAny<ClaimsPrincipal>())).Returns(platformAdmin);
            roles.SetupGet(r => r.AdminRoleName).Returns("Admin");
            ctx.AddTestServices(s => s.AddSingleton(roles.Object));
        };

    private static Mock<ISignInLog> Log(IGenericResult<IReadOnlyList<SignInRecord>> answer)
    {
        var log = new Mock<ISignInLog>();
        log.Setup(l => l.SignIns(Person, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(answer);
        return log;
    }

    private sealed class ListSignIns(UserConfigurationProvider users, ISignInLog signIns)
        : ListUserSignInsEndpointBase(NullLogger.Instance, users, signIns);

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AnAdministratorReadsAPageNewestFirstWithWhoWasCalling()
    {
        var log = Log(GenericResult<IReadOnlyList<SignInRecord>>.Success(Records(3)));
        var endpoint = Factory.Create<ListSignIns>(Caller(Guid.NewGuid().ToString(), platformAdmin: true), Store().Provider, log.Object);

        await endpoint.HandleAsync(new ListUserSignInsRequest { IdOrName = Person.ToString(), Skip = 0, Take = 10 }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Items.Count.ShouldBe(3);
        endpoint.Response.HasMore.ShouldBeFalse();
        var first = endpoint.Response.Items[0];
        first.SignedInAt.ShouldBe(At);
        first.FlowName.ShouldBe("password-session");
        first.IpAddress.ShouldBe("203.0.113.9");
        first.Browser.ShouldBe("Firefox");
        first.OperatingSystem.ShouldBe("Linux");
        first.UserAgent.ShouldBe("Mozilla/5.0 Firefox/132.0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task APersonReadsTheirOwnSignInsWithoutBeingAnAdministrator()
    {
        var log = Log(GenericResult<IReadOnlyList<SignInRecord>>.Success(Records(1)));
        var endpoint = Factory.Create<ListSignIns>(Caller(Person.ToString(), platformAdmin: false), Store().Provider, log.Object);

        await endpoint.HandleAsync(new ListUserSignInsRequest { IdOrName = Person.ToString(), Skip = 0, Take = 10 }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Items.ShouldHaveSingleItem();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task SomeoneWhoIsNeitherThePersonNorTheirAdministratorIsRefusedAndNothingIsRead()
    {
        var log = new Mock<ISignInLog>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListSignIns>(Caller(Guid.NewGuid().ToString(), platformAdmin: false), Store().Provider, log.Object);

        await endpoint.HandleAsync(new ListUserSignInsRequest { IdOrName = Person.ToString(), Skip = 0, Take = 10 }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
        log.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AnAccountThatDoesNotExistAnswersNotFound()
    {
        var log = new Mock<ISignInLog>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListSignIns>(Caller(Guid.NewGuid().ToString(), platformAdmin: true), Store().Provider, log.Object);

        await endpoint.HandleAsync(new ListUserSignInsRequest { IdOrName = Guid.NewGuid().ToString(), Skip = 0, Take = 10 }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
        log.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task APageAsksForOneRowMoreThanItReturnsToKnowWhetherOlderOnesFollow()
    {
        var log = Log(GenericResult<IReadOnlyList<SignInRecord>>.Success(Records(6)));
        var endpoint = Factory.Create<ListSignIns>(Caller(Guid.NewGuid().ToString(), platformAdmin: true), Store().Provider, log.Object);

        await endpoint.HandleAsync(new ListUserSignInsRequest { IdOrName = Person.ToString(), Skip = 10, Take = 5 }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Items.Count.ShouldBe(5);
        endpoint.Response.HasMore.ShouldBeTrue();
        endpoint.Response.Skip.ShouldBe(10);
        endpoint.Response.Take.ShouldBe(5);
        log.Verify(l => l.SignIns(Person, 10, 6, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [InlineData(-1, 10)]
    [InlineData(0, 0)]
    [InlineData(0, 201)]
    public async Task AnOutOfRangeSkipOrTakeIsRefusedAndNotCorrected(int skip, int take)
    {
        var log = new Mock<ISignInLog>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListSignIns>(Caller(Guid.NewGuid().ToString(), platformAdmin: true), Store().Provider, log.Object);

        await endpoint.HandleAsync(new ListUserSignInsRequest { IdOrName = Person.ToString(), Skip = skip, Take = take }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(400);
        log.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AListThatCannotBeReadIsAFailureAndNotAnEmptyList()
    {
        var log = Log(GenericResult<IReadOnlyList<SignInRecord>>.Failure(new GenericMessage("the table could not be read")));
        var endpoint = Factory.Create<ListSignIns>(Caller(Guid.NewGuid().ToString(), platformAdmin: true), Store().Provider, log.Object);

        await endpoint.HandleAsync(new ListUserSignInsRequest { IdOrName = Person.ToString(), Skip = 0, Take = 10 }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(500);
    }
}
