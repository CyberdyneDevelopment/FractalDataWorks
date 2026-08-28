namespace Fdw.Web.Http.Authentication;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Extension methods for registering bearer token authentication handlers.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="BearerTokenHandler"/> and its token provider.
    /// </summary>
    /// <typeparam name="TProvider">The access token provider implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBearerTokenHandler<TProvider>(this IServiceCollection services)
        where TProvider : class, IAccessTokenProvider
    {
        services.TryAddScoped<IAccessTokenProvider, TProvider>();
        services.AddTransient<BearerTokenHandler>();
        return services;
    }

    /// <summary>
    /// Registers the <see cref="RetryingBearerTokenHandler"/> with its token provider and refresh handler.
    /// </summary>
    /// <typeparam name="TProvider">The access token provider implementation type.</typeparam>
    /// <typeparam name="TRefresh">The token refresh handler implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRetryingBearerTokenHandler<TProvider, TRefresh>(this IServiceCollection services)
        where TProvider : class, IAccessTokenProvider
        where TRefresh : class, ITokenRefreshHandler
    {
        services.TryAddScoped<IAccessTokenProvider, TProvider>();
        services.TryAddScoped<ITokenRefreshHandler, TRefresh>();
        services.AddTransient<RetryingBearerTokenHandler>();
        return services;
    }

    /// <summary>
    /// Adds the <see cref="BearerTokenHandler"/> to the HTTP client pipeline.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <returns>The HTTP client builder for chaining.</returns>
    public static IHttpClientBuilder AddBearerTokenHandler(this IHttpClientBuilder builder)
    {
        if (builder is null) throw new System.ArgumentNullException(nameof(builder));

        builder.Services.TryAddTransient<BearerTokenHandler>();
        return builder.AddHttpMessageHandler<BearerTokenHandler>();
    }

    /// <summary>
    /// Adds the <see cref="RetryingBearerTokenHandler"/> to the HTTP client pipeline.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <returns>The HTTP client builder for chaining.</returns>
    public static IHttpClientBuilder AddRetryingBearerTokenHandler(this IHttpClientBuilder builder)
    {
        if (builder is null) throw new System.ArgumentNullException(nameof(builder));

        builder.Services.TryAddTransient<RetryingBearerTokenHandler>();
        return builder.AddHttpMessageHandler<RetryingBearerTokenHandler>();
    }

    /// <summary>
    /// Registers the <see cref="DefaultTokenRefreshCoordinator"/> as the
    /// <see cref="ITokenRefreshCoordinator"/> implementation.
    /// Uses <c>TryAddScoped</c> so consumers can register a custom implementation first.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTokenRefreshCoordinator(this IServiceCollection services)
    {
        services.TryAddScoped<ITokenRefreshCoordinator, DefaultTokenRefreshCoordinator>();
        return services;
    }

    /// <summary>
    /// Registers the <see cref="ApiKeyDelegatingHandler"/> and its API key provider.
    /// Use this instead of <see cref="AddBearerTokenHandler{TProvider}"/> when the client
    /// authenticates with a static API key rather than JWT tokens.
    /// </summary>
    /// <typeparam name="TProvider">The API key provider implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiKeyHandler<TProvider>(this IServiceCollection services)
        where TProvider : class, IApiKeyProvider
    {
        services.TryAddScoped<IApiKeyProvider, TProvider>();
        services.AddTransient<ApiKeyDelegatingHandler>();
        return services;
    }

    /// <summary>
    /// Adds the <see cref="ApiKeyDelegatingHandler"/> to the HTTP client pipeline.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <returns>The HTTP client builder for chaining.</returns>
    public static IHttpClientBuilder AddApiKeyHandler(this IHttpClientBuilder builder)
    {
        if (builder is null) throw new System.ArgumentNullException(nameof(builder));

        builder.Services.TryAddTransient<ApiKeyDelegatingHandler>();
        return builder.AddHttpMessageHandler<ApiKeyDelegatingHandler>();
    }
}
