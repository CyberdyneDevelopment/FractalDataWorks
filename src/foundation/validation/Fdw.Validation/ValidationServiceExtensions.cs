using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fdw.Validation;

/// <summary>
/// Extension methods for registering FDW validation services.
/// </summary>
public static class ValidationServiceExtensions
{
    /// <summary>
    /// Registers all validators from the specified assemblies with the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for validators.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFrameworkValidation(
        this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddValidatorsFromAssemblies(assemblies, ServiceLifetime.Scoped);
        return services;
    }

    /// <summary>
    /// Registers an <see cref="FdwConfigurationValidator{T}"/> as an
    /// <see cref="IValidateOptions{TOptions}"/> for startup validation.
    /// </summary>
    /// <typeparam name="T">The configuration type.</typeparam>
    /// <typeparam name="TValidator">The validator type.</typeparam>
    /// <param name="builder">The options builder.</param>
    /// <returns>The options builder for chaining.</returns>
    public static OptionsBuilder<T> ValidateWithFdw<T, TValidator>(
        this OptionsBuilder<T> builder)
        where T : class
        where TValidator : FdwConfigurationValidator<T>, new()
    {
        builder.Services.AddSingleton<IValidateOptions<T>>(new TValidator());
        return builder;
    }
}
