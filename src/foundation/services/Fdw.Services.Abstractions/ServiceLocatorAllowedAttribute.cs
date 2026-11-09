using System;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Allows a runtime service locator (FDW061, FDW-4618 rule 6) in the marked class, member or parameter, for a stated
/// reason: an interface that demands an <see cref="IServiceProvider"/> (<c>IHealthCheckable.CheckHealth</c>), or the
/// one scope runner (FDW-4639). What is resolved through it must still be provider interfaces.
/// </summary>
/// <remarks>The reason is required; FDW061 reports a mark whose reason is empty, and such a mark allows nothing.</remarks>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method
    | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
    AllowMultiple = false,
    Inherited = false)]
public sealed class ServiceLocatorAllowedAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="ServiceLocatorAllowedAttribute"/> class.</summary>
    /// <param name="reason">Why a runtime service locator is needed here.</param>
    public ServiceLocatorAllowedAttribute(string reason)
    {
        Reason = reason;
    }

    /// <summary>Why a runtime service locator is needed here.</summary>
    public string Reason { get; }
}
