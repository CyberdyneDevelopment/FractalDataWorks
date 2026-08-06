using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// TypeCollection for canvas port directions.
/// </summary>
/// <remarks>
/// <para>
/// Seeded members: <c>In</c> and <c>Out</c>.
/// Downstream assemblies add further directions via <c>[TypeOption]</c> on their own classes.
/// </para>
/// <para>
/// Usage:
/// <code>
/// // O(1) lookup — compare against NotFound sentinel
/// var direction = PortDirections.ByName("In");
/// if (direction == PortDirections.NotFound) { /* handle missing */ }
///
/// // Enumerate all registered directions
/// foreach (var d in PortDirections.All()) { ... }
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(PortDirectionBase), typeof(IPortDirection), typeof(PortDirections))]
[ExcludeFromCodeCoverage]
public abstract partial class PortDirections : TypeCollectionBase<PortDirectionBase, IPortDirection>
{
}
