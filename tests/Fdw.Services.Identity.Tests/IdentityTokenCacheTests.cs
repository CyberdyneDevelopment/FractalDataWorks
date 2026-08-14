using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Identity;
using Fdw.Services.Identity.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity.Tests;

/// <summary>
/// Tests for <see cref="IdentityTokenCache"/> — the behaviour that keeps the identity provider off
/// the hot path of every outbound call.
/// </summary>
public class IdentityTokenCacheTests
{
    private const string Configuration = "SchedulerIdentity";
    private const string Audience = "https://etl.example.dev";

    private static IdentityTokenCache Cache(TimeSpan? skew = null)
        => new(NullLogger<IdentityTokenCache>.Instance, skew ?? TimeSpan.FromSeconds(60));

    private static IssuedIdentityToken Token(TimeSpan validFor, string value = "token-value")
        => new(value, "Bearer", "https://login.example.dev", Audience, DateTimeOffset.UtcNow + validFor);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task GetOrAcquireReusesALiveTokenInsteadOfAcquiringAgain()
    {
        var cache = Cache();
        var request = new IdentityTokenRequest(Audience);
        var acquisitions = 0;

        Task<IGenericResult<IssuedIdentityToken>> Acquire(CancellationToken ct)
        {
            Interlocked.Increment(ref acquisitions);
            return Task.FromResult(GenericResult<IssuedIdentityToken>.Success(Token(TimeSpan.FromMinutes(10))));
        }

        await cache.GetOrAcquire(Configuration, request, Acquire, TestContext.Current.CancellationToken);
        await cache.GetOrAcquire(Configuration, request, Acquire, TestContext.Current.CancellationToken);

        acquisitions.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task GetOrAcquireDoesNotServeATokenInsideTheRefreshSkew()
    {
        // Why 30s of life against a 60s skew: the token has not expired, but it would expire
        // mid-flight. Serving it is the failure this skew exists to prevent.
        var cache = Cache(TimeSpan.FromSeconds(60));
        var request = new IdentityTokenRequest(Audience);
        var acquisitions = 0;

        Task<IGenericResult<IssuedIdentityToken>> Acquire(CancellationToken ct)
        {
            Interlocked.Increment(ref acquisitions);
            return Task.FromResult(GenericResult<IssuedIdentityToken>.Success(Token(TimeSpan.FromSeconds(30))));
        }

        await cache.GetOrAcquire(Configuration, request, Acquire, TestContext.Current.CancellationToken);
        await cache.GetOrAcquire(Configuration, request, Acquire, TestContext.Current.CancellationToken);

        acquisitions.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task GetOrAcquireNeverServesATokenAcrossAudiences()
    {
        // A token minted for one peer must never be handed to a call bound for another.
        var cache = Cache();
        var acquired = new List<string>();

        Task<IGenericResult<IssuedIdentityToken>> AcquireFor(string audience) => Task.FromResult(
            GenericResult<IssuedIdentityToken>.Success(
                new IssuedIdentityToken($"token-for-{audience}", "Bearer", "https://login.example.dev", audience, DateTimeOffset.UtcNow.AddMinutes(10))));

        var first = await cache.GetOrAcquire(Configuration, new IdentityTokenRequest("https://etl.example.dev"),
            _ => AcquireFor("https://etl.example.dev"), TestContext.Current.CancellationToken);
        var second = await cache.GetOrAcquire(Configuration, new IdentityTokenRequest("https://api.example.dev"),
            _ => AcquireFor("https://api.example.dev"), TestContext.Current.CancellationToken);

        first.Value!.Value.ShouldBe("token-for-https://etl.example.dev");
        second.Value!.Value.ShouldBe("token-for-https://api.example.dev");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task GetOrAcquireAcquiresOnceWhenManyCallersArriveTogether()
    {
        // Why this matters: without the per-key gate, a token ageing out makes every in-flight
        // request hit the identity provider at the same instant.
        var cache = Cache();
        var request = new IdentityTokenRequest(Audience);
        var acquisitions = 0;

        async Task<IGenericResult<IssuedIdentityToken>> Acquire(CancellationToken ct)
        {
            Interlocked.Increment(ref acquisitions);
            await Task.Delay(50, ct);
            return GenericResult<IssuedIdentityToken>.Success(Token(TimeSpan.FromMinutes(10)));
        }

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ =>
            cache.GetOrAcquire(Configuration, request, Acquire, TestContext.Current.CancellationToken)));

        acquisitions.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task InvalidateForcesTheNextCallToAcquireAgain()
    {
        // A peer can reject a token the provider revoked before its expiry; the cache must not keep
        // serving a token that is known to fail until the clock catches up.
        var cache = Cache();
        var request = new IdentityTokenRequest(Audience);
        var acquisitions = 0;

        Task<IGenericResult<IssuedIdentityToken>> Acquire(CancellationToken ct)
        {
            Interlocked.Increment(ref acquisitions);
            return Task.FromResult(GenericResult<IssuedIdentityToken>.Success(Token(TimeSpan.FromMinutes(10))));
        }

        await cache.GetOrAcquire(Configuration, request, Acquire, TestContext.Current.CancellationToken);
        cache.Invalidate(Configuration, request);
        await cache.GetOrAcquire(Configuration, request, Acquire, TestContext.Current.CancellationToken);

        acquisitions.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task GetOrAcquireDoesNotCacheAFailure()
    {
        var cache = Cache();
        var request = new IdentityTokenRequest(Audience);
        var attempts = 0;

        Task<IGenericResult<IssuedIdentityToken>> Acquire(CancellationToken ct)
        {
            attempts++;
            return Task.FromResult(attempts == 1
                ? GenericResult<IssuedIdentityToken>.Failure(new GenericMessage(MessageSeverity.Error, "provider down"))
                : GenericResult<IssuedIdentityToken>.Success(Token(TimeSpan.FromMinutes(10))));
        }

        var first = await cache.GetOrAcquire(Configuration, request, Acquire, TestContext.Current.CancellationToken);
        var second = await cache.GetOrAcquire(Configuration, request, Acquire, TestContext.Current.CancellationToken);

        first.IsFailure.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue();
    }
}
