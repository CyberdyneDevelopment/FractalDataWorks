using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Components.Services;

/// <summary>
/// Represents a status display badge with label, color, and variant information.
/// </summary>
/// <param name="Label">The display text for the badge.</param>
/// <param name="Color">The semantic color.</param>
/// <param name="Variant">The semantic variant.</param>
[ExcludeFromCodeCoverage]
public sealed record StatusBadge(string Label, IStatusColor Color, IStatusVariant Variant);
