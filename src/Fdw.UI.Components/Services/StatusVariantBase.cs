using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Components.Services;

/// <summary>
/// Base class for semantic status variants for status badges. Each variant carries the one css class
/// that colours it, so the tone-to-class mapping exists once instead of per component.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class StatusVariantBase : TypeOptionBase<int, StatusVariantBase>, IStatusVariant
{
    /// <summary>
    /// Initializes a new instance of <see cref="StatusVariantBase"/>.
    /// </summary>
    protected StatusVariantBase(int id, string name, string badgeClass) : base(id, name)
    {
        BadgeClass = badgeClass;
    }

    /// <inheritdoc/>
    public string BadgeClass { get; }
}
