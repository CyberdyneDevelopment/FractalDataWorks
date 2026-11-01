using System;
using Fdw.Services.Authentication.Abstractions.SignIns;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Authentication.SignIns;

/// <summary>
/// Describes the caller of the HTTP request in hand (FDW-2729): the address it came from and the User-Agent it sent.
/// </summary>
/// <remarks>
/// The address is the connection's remote address as the pipeline holds it: the forwarded-headers host type, which runs first, replaces it with the
/// address X-Forwarded-For names, up to the number of proxies that host counts. Behind more reverse proxies than it counts, the address is a proxy's and
/// not the browser's. An IPv4 address that arrives mapped into IPv6 is written as plain IPv4. The address is cut to 45 characters and the User-Agent
/// to 512, the widths of the columns they are stored in. With no request in hand (a TDS login) the caller is not known.
/// </remarks>
public sealed class HttpSignInCaller : ISignInCaller
{
    private const int MaxAddress = 45;
    private const int MaxUserAgent = 512;

    private readonly IHttpContextAccessor _accessor;

    /// <summary>Initializes a new instance of the <see cref="HttpSignInCaller"/> class.</summary>
    /// <param name="accessor">Reaches the request in hand.</param>
    public HttpSignInCaller(IHttpContextAccessor accessor)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    /// <inheritdoc />
    public SignInCaller Current()
    {
        var context = _accessor.HttpContext;
        if (context is null)
            return SignInCaller.NotKnown;

        var address = context.Connection.RemoteIpAddress;
        var agent = context.Request.Headers.UserAgent.ToString();

        return new SignInCaller
        {
            IpAddress = address is null ? null : Cut(address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString(), MaxAddress),
            UserAgent = string.IsNullOrWhiteSpace(agent) ? null : Cut(agent, MaxUserAgent),
        };
    }

    private static string Cut(string text, int length) => text.Length <= length ? text : text[..length];
}
