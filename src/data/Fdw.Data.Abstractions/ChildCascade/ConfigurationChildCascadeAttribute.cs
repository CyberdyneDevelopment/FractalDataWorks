using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data;

/// <summary>
/// Declares how saving the owner treats a child collection whose container hangs from the owner's durable Id (FDW-4240):
/// <see cref="AuthoritativeChildCascade"/> (the list is the full set) or <see cref="UpsertOnlyChildCascade"/> (the list only adds).
/// </summary>
/// <remarks>
/// A collection whose container hangs from the owner's row (its Physical key) needs no mode: it is retired with the replaced version and
/// written again. For a collection that hangs from the Id there is no default; a save of an owner whose collection declares no mode is
/// refused. The generator reads the option type by its full name.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class ConfigurationChildCascadeAttribute : Attribute
{
    /// <summary>Initializes the attribute with the mode's option type.</summary>
    /// <param name="mode">The option type of <see cref="ChildCascadeModes"/>: <see cref="AuthoritativeChildCascade"/> or <see cref="UpsertOnlyChildCascade"/>.</param>
    public ConfigurationChildCascadeAttribute(Type mode)
    {
        Mode = mode;
    }

    /// <summary>The option type of <see cref="ChildCascadeModes"/> this collection declares.</summary>
    public Type Mode { get; }
}
