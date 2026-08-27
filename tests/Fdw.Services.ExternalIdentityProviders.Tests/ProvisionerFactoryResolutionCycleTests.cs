using System;
using System.Linq;
using System.Reflection;
using Fdw.ServiceTypes;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Chained;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

using Fdw.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.Tests;

/// <summary>
/// Regression guard for the provider-realization recursion defect.
/// </summary>
/// <remarks>
/// <para>
/// The source-generated scoped resolver for a domain provider runs each option's
/// <c>RegisterFactory(provider, sp)</c> INSIDE the resolver lambda. If an option's
/// <c>RegisterFactory</c> resolves a service whose constructor depends — directly or transitively — on
/// that same provider, MEDI re-enters the lambda (its cache entry is not published yet) and recurses
/// without bound. MEDI's <c>StackGuard.RunOnEmptyStack</c> migrates that recursion onto fresh stacks
/// rather than throwing <see cref="StackOverflowException"/>, so the host HANGS SILENTLY — no
/// exception, no log — until the container runtime kills it. A production dump showed ~83,000 frames
/// cycling through <c>ExternalIdentityProvisionerTypes.&lt;Register&gt;b__16_1</c> →
/// <c>ChainedExternalIdentityProvisionerType.RegisterFactory</c> → <c>GetRequiredService</c> → repeat.
/// </para>
/// <para>
/// The break is to take the provider as <see cref="Lazy{T}"/> so resolution is deferred past
/// construction. These tests pin that contract by reflection: they are deterministic, need no
/// container, and fail on the pre-fix code.
/// </para>
/// </remarks>
public sealed class ProvisionerFactoryResolutionCycleTests
{
    // Why: the exact provider service type whose realization the factory must NOT re-enter.
    private static readonly Type ProviderServiceType =
        typeof(IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerConfiguration>);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ChainedProvisionerFactoryDoesNotTakeItsOwnProviderDirectly()
    {
        // Arrange
        var constructor = typeof(ChainedExternalIdentityProvisionerFactory)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single();

        // Act
        var direct = constructor.GetParameters().Where(p => p.ParameterType == ProviderServiceType).ToList();

        // Assert
        direct.ShouldBeEmpty(
            "ChainedExternalIdentityProvisionerFactory must not take its own collection's provider as a "
            + "direct constructor dependency — it is resolved from inside that provider's generated scoped "
            + "resolver lambda, so a direct dependency re-enters the lambda and recurses until the host is "
            + "killed (silently — StackGuard suppresses StackOverflowException). Use Lazy<T>.");
    }

    // Why: the system rule is stronger than "defer the dependency" — a factory must be a PURE
    // constructor that holds no provider at all (the provider passes itself to Create). A Lazy<provider>
    // ctor param would also be rejected here: it makes the deviation survivable instead of removing it.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ChainedProvisionerFactoryIsAPureConstructor()
    {
        // Arrange
        var constructor = typeof(ChainedExternalIdentityProvisionerFactory)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single();

        // Act
        var nonLoggerParameters = constructor.GetParameters()
            .Where(p => p.ParameterType != typeof(ILoggerFactory))
            .Select(p => $"{p.ParameterType.Name} {p.Name}")
            .ToList();

        // Assert
        nonLoggerParameters.ShouldBeEmpty(
            "a provisioner factory must be a pure constructor — the provider supplies resolved values to "
            + "Create(configuration, provisionerProvider). Holding a provider (even as Lazy<T>) keeps the "
            + "deviation alive instead of removing it. Offending parameters: "
            + string.Join(", ", nonLoggerParameters));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ProvisionerFactoryContractExposesProviderSuppliedCreate()
    {
        // Arrange
        var factoryInterface = typeof(IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>);

        // Act
        var providerSuppliedCreate = factoryInterface.GetMethods()
            .SingleOrDefault(m => m.Name == "Create"
                                  && m.GetParameters().Length == 2
                                  && m.GetParameters()[1].ParameterType == ProviderServiceType);

        // Assert
        providerSuppliedCreate.ShouldNotBeNull(
            "the factory contract must expose Create(configuration, provisionerProvider) so the provider "
            + "can hand over the already-resolved provider instead of the factory resolving it.");
    }

    // Why: generalises the guard to the whole assembly so a NEW option cannot reintroduce the same
    // cycle. Any constructor parameter typed as a bare IPlatformServiceProvider<,> on a factory is the
    // root risk signature; Lazy<>/Func<> wrapped dependencies are safe because they defer resolution.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NoProvisionerFactoryTakesAFdwServiceProviderDirectly()
    {
        // Arrange
        var factoryTypes = typeof(ChainedExternalIdentityProvisionerFactory).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.Name.EndsWith("Factory", StringComparison.Ordinal))
            .ToList();

        // Act
        var offenders = factoryTypes
            .SelectMany(t => t.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(c => c.GetParameters())
                .Where(p => p.ParameterType.IsGenericType
                            && p.ParameterType.GetGenericTypeDefinition() == typeof(IPlatformServiceProvider<,>))
                .Select(p => $"{t.Name}({p.ParameterType.Name} {p.Name})"))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty(
            "a factory taking IPlatformServiceProvider<,> directly re-enters that provider's generated scoped "
            + "resolver lambda when the option's RegisterFactory resolves it, causing an unbounded, silent "
            + "recursion. Wrap the dependency in Lazy<T>. Offenders: " + string.Join(", ", offenders));
    }
}
