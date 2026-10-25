using System;
using System.Net;
using Fdw.Services.Authentication.Abstractions.SignIns;
using Fdw.Services.Authentication.SignIns;
using Microsoft.AspNetCore.Http;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.SignIns;

/// <summary>
/// FDW-2729: the caller of the request in hand is its remote address and its User-Agent, each cut to the width of the column it is stored in,
/// and a host with no request has no caller.
/// </summary>
public sealed class HttpSignInCallerTests
{
    private static HttpSignInCaller CallerOf(HttpContext? context)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(context);
        return new HttpSignInCaller(accessor.Object);
    }

    private static DefaultHttpContext Request(string? address, string? userAgent)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = address is null ? null : IPAddress.Parse(address);
        if (userAgent is not null)
            context.Request.Headers.UserAgent = userAgent;
        return context;
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void TheCallerIsTheRemoteAddressAndTheUserAgentOfTheRequest()
    {
        var caller = CallerOf(Request("203.0.113.9", "Mozilla/5.0 Firefox/132.0")).Current();

        caller.IpAddress.ShouldBe("203.0.113.9");
        caller.UserAgent.ShouldBe("Mozilla/5.0 Firefox/132.0");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void AnIpv4AddressMappedIntoIpv6IsWrittenAsPlainIpv4()
        => CallerOf(Request("::ffff:203.0.113.9", "x")).Current().IpAddress.ShouldBe("203.0.113.9");

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void AnIpv6AddressIsKept()
        => CallerOf(Request("2001:db8::1", "x")).Current().IpAddress.ShouldBe("2001:db8::1");

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void ARequestWithNoUserAgentOrAddressHasNeither()
    {
        var caller = CallerOf(Request(null, null)).Current();

        caller.IpAddress.ShouldBeNull();
        caller.UserAgent.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void AUserAgentLongerThanItsColumnIsCut()
        => CallerOf(Request("203.0.113.9", new string('a', 600))).Current().UserAgent!.Length.ShouldBe(512);

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void AHostWithNoRequestHasNoCaller()
        => CallerOf(null).Current().ShouldBeSameAs(SignInCaller.NotKnown);

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void AMissingAccessorIsRefused()
        => Should.Throw<ArgumentNullException>(() => new HttpSignInCaller(null!));
}
