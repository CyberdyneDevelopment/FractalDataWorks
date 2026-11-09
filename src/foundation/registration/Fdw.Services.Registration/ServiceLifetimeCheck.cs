using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fdw.Messages;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.ServiceTypes;

/// <summary>
/// Walks a host's registrations after the Register phase and names every singleton that takes a scoped or a transient service in its constructor (FDW-4640).
/// </summary>
/// <remarks>
/// <para>
/// The framework registers some singletons that take its own transient services by design (IOptions&lt;T&gt; and IOptionsMonitor&lt;T&gt; take the transient IOptionsFactory&lt;T&gt;),
/// so the caller passes the list of service types the walk skips; <see cref="PlatformServices.LifetimeCheckSkippedServiceTypes"/> declares it.
/// </para>
/// <para>
/// Why this exists beside <c>ValidateScopes</c> and <c>ValidateOnBuild</c>: ValidateScopes refuses a scoped service resolved from the root provider, and ValidateOnBuild checks
/// the constructors of registrations that name an implementation type; neither looks inside a <c>sp =&gt; new X(...)</c> registration, and a singleton that takes a transient
/// service passes both. This walk reads each singleton's chosen constructor and the lifetime registered for each parameter, so both cases are named when the host starts.
/// </para>
/// <para>
/// What it cannot see: a factory registration names no implementation type, so its dependencies are not read here; the container test that resolves every singleton covers
/// those. A keyed registration is skipped for the same reason a key cannot be looked up from a constructor parameter type.
/// </para>
/// </remarks>
public static class ServiceLifetimeCheck
{
    /// <summary>Checks the registrations.</summary>
    /// <param name="services">The registrations of the host, as the Register phase left them.</param>
    /// <param name="logger">The logger the findings are written to.</param>
    /// <param name="skipped">Service types the walk does not report as a singleton's dependency; an open generic names every closed form of it.</param>
    /// <returns>One coded message for each singleton constructor parameter whose registered lifetime is scoped or transient; empty when there is none.</returns>
    public static IReadOnlyList<IGenericMessage> Check(IServiceCollection services, ILogger logger, IReadOnlyCollection<Type> skipped)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (logger is null) throw new ArgumentNullException(nameof(logger));
        if (skipped is null) throw new ArgumentNullException(nameof(skipped));

        var registered = services.Where(d => !d.IsKeyedService).ToList();
        var byType = registered.GroupBy(d => d.ServiceType).ToDictionary(g => g.Key, g => g.ToList());
        var findings = new List<IGenericMessage>();

        foreach (var singleton in registered.Where(d => d.Lifetime == ServiceLifetime.Singleton))
        {
            if (singleton.ImplementationType is not { IsAbstract: false } implementation)
            {
                continue;
            }

            if (ChosenConstructor(implementation, byType) is not { } constructor)
            {
                continue;
            }

            foreach (var parameter in constructor.GetParameters().Where(p => !IsSkipped(p.ParameterType, skipped)))
            {
                var lifetime = LifetimeOf(parameter.ParameterType, byType);
                if (lifetime == ServiceLifetime.Scoped)
                {
                    findings.Add(ServiceTypeLog.SingletonTakesScopedService(
                        logger, singleton.ServiceType.ToString(), implementation.ToString(),
                        parameter.ParameterType.ToString(), parameter.Name ?? string.Empty));
                }
                else if (lifetime == ServiceLifetime.Transient)
                {
                    findings.Add(ServiceTypeLog.SingletonTakesTransientService(
                        logger, singleton.ServiceType.ToString(), implementation.ToString(),
                        parameter.ParameterType.ToString(), parameter.Name ?? string.Empty));
                }
            }
        }

        return findings;
    }

    // A closed generic is skipped when its open form is listed, as IOptionsFactory<Foo> is by IOptionsFactory<>.
    private static bool IsSkipped(Type parameter, IReadOnlyCollection<Type> skipped) =>
        skipped.Contains(parameter) || (parameter.IsGenericType && skipped.Contains(parameter.GetGenericTypeDefinition()));

    // The constructor the container picks: the one with the most parameters of those it can fill. A type with none it can fill is left to ValidateOnBuild to refuse.
    private static ConstructorInfo? ChosenConstructor(Type implementation, Dictionary<Type, List<ServiceDescriptor>> byType) =>
        implementation.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault(c => c.GetParameters().All(p => p.HasDefaultValue || IsKnown(p.ParameterType, byType)));

    private static bool IsKnown(Type type, Dictionary<Type, List<ServiceDescriptor>> byType) =>
        type == typeof(IServiceProvider) || type == typeof(IServiceScopeFactory) || IsEnumerable(type) || Find(type, byType) is not null;

    private static bool IsEnumerable(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);

    private static List<ServiceDescriptor>? Find(Type type, Dictionary<Type, List<ServiceDescriptor>> byType)
    {
        if (byType.TryGetValue(type, out var exact))
        {
            return exact;
        }

        return type.IsGenericType && byType.TryGetValue(type.GetGenericTypeDefinition(), out var open) ? open : null;
    }

    // The lifetime the container gives a parameter. A single service is the last one registered; a sequence is every one, and one scoped or transient member decides it.
    private static ServiceLifetime? LifetimeOf(Type parameter, Dictionary<Type, List<ServiceDescriptor>> byType)
    {
        if (IsEnumerable(parameter))
        {
            var members = Find(parameter.GetGenericArguments()[0], byType);
            return members is null ? null
                : members.Any(d => d.Lifetime == ServiceLifetime.Scoped) ? ServiceLifetime.Scoped
                : members.Any(d => d.Lifetime == ServiceLifetime.Transient) ? ServiceLifetime.Transient
                : ServiceLifetime.Singleton;
        }

        return Find(parameter, byType)?.Last().Lifetime;
    }
}
