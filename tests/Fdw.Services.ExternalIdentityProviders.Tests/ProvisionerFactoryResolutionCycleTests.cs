using System;
using System.Linq;
using System.Reflection;
using Fdw.ServiceTypes;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.ClaimMapped;
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
/// cycling through the (now-removed) Chained provisioner's own factory resolution, the option that
/// first hit this. Its two dedicated regression tests were removed along with it; the general guard
/// below stays, since the hazard is generic to ANY provisioner factory, not specific to that option.
/// </para>
/// <para>
/// The break is to take the provider as <see cref="Lazy{T}"/> so resolution is deferred past
/// construction. This test pins that contract by reflection: it is deterministic, needs no
/// container, and fails on a factory reintroducing the same mistake.
/// </para>
/// </remarks>
public sealed class ProvisionerFactoryResolutionCycleTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NoProvisionerFactoryTakesAFdwServiceProviderDirectly()
    {
        // Arrange
        var factoryTypes = typeof(ClaimMappedProvisionerFactory).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.Name.EndsWith("Factory", StringComparison.Ordinal))
            .ToList();

        // Act
        var offenders = factoryTypes
            .SelectMany(t => t.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(c => c.GetParameters())
                .Where(p => p.ParameterType.IsGenericType
                            && p.ParameterType.GetGenericTypeDefinition() == typeof(IDomainServiceProvider<,>))
                .Select(p => $"{t.Name}({p.ParameterType.Name} {p.Name})"))
            .ToList();

        // Assert
        offenders.ShouldBeEmpty(
            "a factory taking IDomainServiceProvider<,> directly re-enters that provider's generated scoped "
            + "resolver lambda when the option's RegisterFactory resolves it, causing an unbounded, silent "
            + "recursion. Wrap the dependency in Lazy<T>. Offenders: " + string.Join(", ", offenders));
    }
}
