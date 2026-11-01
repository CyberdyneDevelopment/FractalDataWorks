using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fdw.Services.RateLimiting.Abstractions;
using Fdw.Services.RateLimiting.Handlers;
using Fdw.Services.RateLimiting.Logging;
using Fdw.Services.Settings;

namespace Fdw.Services.RateLimiting.Extensions;

/// <summary>
/// Extension methods for registering rate limiting services with dependency injection.
/// </summary>
/// <remarks>
/// <para>
/// These extensions integrate Fdw rate limit policies from the
/// <see cref="RateLimitPolicies"/> TypeCollection with ASP.NET Core's built-in
/// rate limiting middleware.
/// </para>
/// <para>
/// All policies defined in the TypeCollection are automatically registered as
/// named rate limiters that can be applied via attributes or middleware configuration.
/// </para>
/// </remarks>
public static class RateLimitingServiceExtensions
{
    /// <summary>
    /// Adds rate limiting services using FDW RateLimitPolicies TypeCollection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">Optional action to further configure rate limiter options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    /// <remarks>
    /// <para>
    /// This method:
    /// </para>
    /// <list type="number">
    /// <item>Iterates all policies from <see cref="RateLimitPolicies.All()"/></item>
    /// <item>Registers each policy as a named rate limiter whose allowance is read from its settings for each request</item>
    /// <item>Sets the rejection handler to <see cref="RateLimitRejectionHandler.HandleRejection"/></item>
    /// <item>Applies any additional configuration from the <paramref name="configure"/> action</item>
    /// </list>
    /// <para>
    /// The host must also register the platform's settings (<see cref="IEffectiveSettingsProvider"/>) and have each policy's
    /// <c>RateLimit.{policy}.*</c> settings in its baseline runtime script: a request under a policy whose setting is absent
    /// is refused with a 500 that names the setting, never given a made-up allowance (FDW-1945).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In Program.cs or Startup.cs
    /// builder.Services.AddFrameworkRateLimiting();
    ///
    /// // Or with additional configuration
    /// builder.Services.AddFrameworkRateLimiting(options =>
    /// {
    ///     options.RejectionStatusCode = 429;
    /// });
    ///
    /// // Later in the pipeline
    /// app.UseRateLimiter();
    ///
    /// // Apply to endpoints
    /// app.MapGet("/api/data", () => "Hello")
    ///    .RequireRateLimiting("Standard");
    /// </code>
    /// </example>
    public static IServiceCollection AddFrameworkRateLimiting(
        this IServiceCollection services,
        Action<RateLimiterOptions>? configure = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddRateLimiter(options =>
        {
            // Register all policies from TypeCollection
            var policies = RateLimitPolicies.All();
            foreach (var policy in policies)
            {
                RegisterPolicy(options, policy);
            }

            // Apply any additional configuration
            configure?.Invoke(options);

            // Set rejection handler to return 429 with Retry-After
            options.OnRejected = RateLimitRejectionHandler.HandleRejection;
        });

        return services;
    }

    /// <summary>
    /// Adds rate limiting services with logging support.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="loggerFactory">The logger factory for logging policy registration.</param>
    /// <param name="configure">Optional action to further configure rate limiter options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    /// <remarks>
    /// <para>
    /// This overload provides logging during policy registration using the provided logger factory.
    /// Use this when you want to see policy registration logs during startup.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddFrameworkRateLimiting(
        this IServiceCollection services,
        ILoggerFactory loggerFactory,
        Action<RateLimiterOptions>? configure = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var logger = loggerFactory?.CreateLogger(typeof(RateLimitingServiceExtensions));

        services.AddRateLimiter(options =>
        {
            // Register all policies from TypeCollection
            var policies = RateLimitPolicies.All();
            foreach (var policy in policies)
            {
                RegisterPolicy(options, policy);

                if (logger != null)
                {
                    RateLimitLog.PolicyRegistered(logger, policy.Name);
                }
            }

            if (logger != null)
            {
                RateLimitLog.AllPoliciesRegistered(logger, policies.Count);
            }

            // Apply any additional configuration
            configure?.Invoke(options);

            // Set rejection handler to return 429 with Retry-After
            options.OnRejected = RateLimitRejectionHandler.HandleRejection;
        });

        return services;
    }

    /// <summary>
    /// Registers a single rate limit policy with the rate limiter options.
    /// </summary>
    /// <param name="options">The rate limiter options to register the policy with.</param>
    /// <param name="policy">The rate limit policy to register.</param>
    /// <remarks>
    /// The policy is registered by its name only. Which algorithm counts its requests, and with what numbers, is not known
    /// until a request arrives: <see cref="PartitionFor"/> reads it from the policy's settings and the algorithm builds its own
    /// limiter, so there is no switch on the algorithm here.
    /// </remarks>
    private static void RegisterPolicy(RateLimiterOptions options, IRateLimitPolicy policy)
    {
        options.AddPolicy(policy.Name, httpContext => PartitionFor(policy, httpContext));
    }

    /// <summary>
    /// Gets the bucket a request counts against under a policy: the one its partitioning names for the request, each with
    /// the allowance the policy's settings give for the request.
    /// </summary>
    /// <param name="policy">The policy the request is counted under.</param>
    /// <param name="httpContext">The request.</param>
    /// <returns>The request's bucket.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the policy or the request is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the policy's allowance cannot be read from its settings (a setting that is not set, or holds a value the
    /// limiter cannot use; the message names it), or when the request has no identity the policy's partitioning needs. It is
    /// never given a made-up allowance or a bucket everyone shares: the limiter runs after authentication, so either is a fault
    /// to find, not a case to absorb.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The allowance is read for each request (FDW-1945), for the company the caller is acting in (FDW-1946), so a setting an
    /// operator changes holds from the next request, and a company's own value for it holds for its callers. The bucket's key
    /// begins with the allowance's fingerprint, so a changed setting starts a new bucket under the new numbers and the old one
    /// is left to age out; a limiter is never edited in place. Two companies held to different numbers never share a bucket for
    /// that reason; two held to the same numbers share exactly as much as the policy's way of counting says (a shared bucket is
    /// still one, each caller's and each address's is still their own).
    /// </para>
    /// <para>
    /// Before TC-380 every policy used its own name as the key, so all callers shared one bucket per policy and one busy
    /// caller, or a restart that made every client retry at once, refused all the others.
    /// </para>
    /// </remarks>
    public static RateLimitPartition<string> PartitionFor(IRateLimitPolicy policy, HttpContext httpContext)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        if (httpContext is null)
        {
            throw new ArgumentNullException(nameof(httpContext));
        }

        var allowance = RateLimitAllowances.Read(
            httpContext.RequestServices.GetRequiredService<IEffectiveSettingsProvider>(),
            policy,
            RateLimitCompany.Of(httpContext.User),
            httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(RateLimitingServiceExtensions)));
        if (allowance.IsFailure)
        {
            throw new InvalidOperationException(allowance.CurrentMessage);
        }

        var held = allowance.Value!;
        return RateLimitPartition.Get(
            held.Fingerprint + "|" + held.Partitioning.KeyOf(policy, httpContext.User, httpContext.Connection.RemoteIpAddress),
            _ => held.Algorithm.CreateLimiter(held));
    }
}
