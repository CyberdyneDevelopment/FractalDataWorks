using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Hosting.Configuration;
using Fdw.Hosting.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Xunit;
using Shouldly;

namespace Fdw.Hosting.Tests;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task InvokeSetsXContentTypeOptionsToNosniff()
    {
        var (context, feature) = CreateContextWithResponseFeature();
        var middleware = CreateMiddleware(new SecurityHeadersOptions());

        await middleware.Invoke(context);
        await feature.FireOnStarting();

        context.Response.Headers["X-Content-Type-Options"].ToString().ShouldBe("nosniff");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task InvokeSetsXFrameOptionsToDenyByDefault()
    {
        var (context, feature) = CreateContextWithResponseFeature();
        var middleware = CreateMiddleware(new SecurityHeadersOptions { AllowFraming = false });

        await middleware.Invoke(context);
        await feature.FireOnStarting();

        context.Response.Headers["X-Frame-Options"].ToString().ShouldBe("DENY");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task InvokeSetsXFrameOptionsToSameoriginWhenAllowFraming()
    {
        var (context, feature) = CreateContextWithResponseFeature();
        var middleware = CreateMiddleware(new SecurityHeadersOptions { AllowFraming = true });

        await middleware.Invoke(context);
        await feature.FireOnStarting();

        context.Response.Headers["X-Frame-Options"].ToString().ShouldBe("SAMEORIGIN");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task InvokeSetsXXssProtectionToZero()
    {
        var (context, feature) = CreateContextWithResponseFeature();
        var middleware = CreateMiddleware(new SecurityHeadersOptions());

        await middleware.Invoke(context);
        await feature.FireOnStarting();

        context.Response.Headers["X-XSS-Protection"].ToString().ShouldBe("0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task InvokeSetsReferrerPolicy()
    {
        var (context, feature) = CreateContextWithResponseFeature();
        var middleware = CreateMiddleware(new SecurityHeadersOptions());

        await middleware.Invoke(context);
        await feature.FireOnStarting();

        context.Response.Headers["Referrer-Policy"].ToString().ShouldBe("strict-origin-when-cross-origin");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task InvokeSetsPermissionsPolicy()
    {
        var (context, feature) = CreateContextWithResponseFeature();
        var middleware = CreateMiddleware(new SecurityHeadersOptions());

        await middleware.Invoke(context);
        await feature.FireOnStarting();

        var policy = context.Response.Headers["Permissions-Policy"].ToString();
        policy.ShouldContain("camera=()");
        policy.ShouldContain("microphone=()");
        policy.ShouldContain("geolocation=()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task InvokeSetsCustomCspWhenProvided()
    {
        var customCsp = "default-src 'none'";
        var (context, feature) = CreateContextWithResponseFeature();
        var middleware = CreateMiddleware(new SecurityHeadersOptions { ContentSecurityPolicy = customCsp });

        await middleware.Invoke(context);
        await feature.FireOnStarting();

        context.Response.Headers["Content-Security-Policy"].ToString().ShouldBe(customCsp);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task InvokeSetsDefaultCspWhenEnabledAndNoCustomCsp()
    {
        var (context, feature) = CreateContextWithResponseFeature();
        var middleware = CreateMiddleware(new SecurityHeadersOptions
        {
            EnableDefaultCsp = true,
            ContentSecurityPolicy = null
        });

        await middleware.Invoke(context);
        await feature.FireOnStarting();

        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.ShouldContain("default-src 'self'");
        csp.ShouldContain("script-src 'self' 'unsafe-inline'");
        csp.ShouldContain("frame-ancestors 'none'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task InvokeDoesNotSetCspWhenDefaultCspDisabledAndNoCustomCsp()
    {
        var (context, feature) = CreateContextWithResponseFeature();
        var middleware = CreateMiddleware(new SecurityHeadersOptions
        {
            EnableDefaultCsp = false,
            ContentSecurityPolicy = null
        });

        await middleware.Invoke(context);
        await feature.FireOnStarting();

        context.Response.Headers.ContainsKey("Content-Security-Policy").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task InvokeSetsCacheControlForSensitivePath()
    {
        var (context, feature) = CreateContextWithResponseFeature();
        context.Request.Path = "/api/v1/auth/login";
        var middleware = CreateMiddleware(new SecurityHeadersOptions());

        await middleware.Invoke(context);
        await feature.FireOnStarting();

        context.Response.Headers["Cache-Control"].ToString().ShouldContain("no-store");
        context.Response.Headers["Pragma"].ToString().ShouldBe("no-cache");
        context.Response.Headers["Expires"].ToString().ShouldBe("0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task InvokeDoesNotSetCacheControlForNonSensitivePath()
    {
        var (context, feature) = CreateContextWithResponseFeature();
        context.Request.Path = "/api/v1/data/query";
        var middleware = CreateMiddleware(new SecurityHeadersOptions());

        await middleware.Invoke(context);
        await feature.FireOnStarting();

        context.Response.Headers.ContainsKey("Cache-Control").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task InvokeSetsCacheControlForCustomSensitivePath()
    {
        var (context, feature) = CreateContextWithResponseFeature();
        context.Request.Path = "/secret/data";
        var middleware = CreateMiddleware(new SecurityHeadersOptions
        {
            SensitivePaths = ["/secret"]
        });

        await middleware.Invoke(context);
        await feature.FireOnStarting();

        context.Response.Headers["Cache-Control"].ToString().ShouldContain("no-store");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task InvokeCallsNextMiddleware()
    {
        var nextCalled = false;
        var middleware = new SecurityHeadersMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            new SecurityHeadersOptions());

        var (context, _) = CreateContextWithResponseFeature();
        await middleware.Invoke(context);

        nextCalled.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task InvokeCustomCspTakesPriorityOverDefaultCsp()
    {
        var customCsp = "default-src 'none'; script-src 'self'";
        var (context, feature) = CreateContextWithResponseFeature();
        var middleware = CreateMiddleware(new SecurityHeadersOptions
        {
            ContentSecurityPolicy = customCsp,
            EnableDefaultCsp = true
        });

        await middleware.Invoke(context);
        await feature.FireOnStarting();

        context.Response.Headers["Content-Security-Policy"].ToString().ShouldBe(customCsp);
    }

    private static SecurityHeadersMiddleware CreateMiddleware(SecurityHeadersOptions options)
    {
        return new SecurityHeadersMiddleware(_ => Task.CompletedTask, options);
    }

    private static (DefaultHttpContext context, TestHttpResponseFeature feature) CreateContextWithResponseFeature()
    {
        var feature = new TestHttpResponseFeature();
        var featureCollection = new FeatureCollection();
        featureCollection.Set<IHttpResponseFeature>(feature);
        featureCollection.Set<IHttpRequestFeature>(new HttpRequestFeature());
        var context = new DefaultHttpContext(featureCollection);
        return (context, feature);
    }

    /// <summary>
    /// Custom IHttpResponseFeature that captures and fires OnStarting callbacks,
    /// since DefaultHttpContext's default feature doesn't expose them for testing.
    /// </summary>
    private sealed class TestHttpResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> callback, object state)> _onStarting = [];

        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted { get; private set; }

        public void OnCompleted(Func<object, Task> callback, object state) { }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            _onStarting.Add((callback, state));
        }

        public async Task FireOnStarting()
        {
            // Fire in reverse order (same as ASP.NET Core)
            for (var i = _onStarting.Count - 1; i >= 0; i--)
            {
                var (callback, state) = _onStarting[i];
                await callback(state).ConfigureAwait(false);
            }
            HasStarted = true;
        }
    }
}
