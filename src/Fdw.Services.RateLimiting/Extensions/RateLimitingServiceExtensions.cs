using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fdw.Services.RateLimiting.Abstractions;
using Fdw.Services.RateLimiting.Handlers;
using Fdw.Services.RateLimiting.Logging;

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
    /// <item>Registers each policy as a named rate limiter based on its <see cref="IRateLimitAlgorithm"/></item>
    /// <item>Sets the rejection handler to <see cref="RateLimitRejectionHandler.HandleRejection"/></item>
    /// <item>Applies any additional configuration from the <paramref name="configure"/> action</item>
    /// </list>
    /// <para>
    /// The algorithm switch is acceptable here because it dispatches on the Algorithm property
    /// of a policy already retrieved from the TypeCollection. We're not using switch to identify
    /// WHICH policy, only HOW to configure the .NET rate limiter based on the policy's algorithm.
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
                    RateLimitLog.PolicyRegistered(logger, policy.Name, policy.Algorithm.Name);
                    RateLimitLog.PolicyConfiguration(
                        logger,
                        policy.Name,
                        policy.RequestsPerWindow,
                        policy.Window.TotalMilliseconds,
                        policy.Algorithm.Name,
                        policy.AllowBurst,
                        policy.BurstLimit);
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
    /// <para>
    /// The switch on Algorithm is intentional - we're dispatching based on the policy's
    /// declared algorithm, not identifying which policy to use. The policy lookup happens
    /// via TypeCollection; this switch only configures HOW to implement that policy.
    /// </para>
    /// </remarks>
    private static void RegisterPolicy(RateLimiterOptions options, IRateLimitPolicy policy)
    {
        options.AddPolicy(policy.Name, _ =>
            RateLimitPartition.Get(policy.Name, _ => policy.Algorithm.CreateLimiter(policy)));
    }

}
