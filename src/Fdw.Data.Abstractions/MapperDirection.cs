#pragma warning disable FDW017 // Attribute parameter enum — TypeCollection not applicable here
namespace Fdw.Data;

/// <summary>
/// Controls which mapping directions a generated POCO mapper emits.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Input"/> (default) preserves backward compatibility — the generated mapper
/// contains only the <c>MapFromReader</c> / <c>MapFromDictionary</c> methods needed to
/// materialise a POCO from a data source.
/// </para>
/// <para>
/// <see cref="Output"/> generates the write-side surface (<c>MapToParameters</c> /
/// <c>MapToWriter</c>) so that a connector or translator can serialise a POCO back to
/// storage without reflection.
/// </para>
/// <para>
/// <see cref="Both"/> generates the full bidirectional surface. Use for configuration
/// classes that participate in both reads and writes.
/// </para>
/// </remarks>
public enum MapperDirection
{
    /// <summary>
    /// Generate only input (read) mapping: <c>MapFromReader</c> and <c>MapFromDictionary</c>.
    /// Default value — backward-compatible with all existing <c>[GenerateMapper]</c> sites.
    /// </summary>
    Input = 0,

    /// <summary>
    /// Generate only output (write) mapping: <c>GetPropertyNames</c> and <c>MapToParameters</c>.
    /// Intended for write-side connectors (<c>ITargetConnector&lt;T&gt;</c>).
    /// </summary>
    Output = 1,

    /// <summary>
    /// Generate both input and output mapping surfaces.
    /// Use for types that participate in both reads and writes.
    /// </summary>
    Both = 2,
}
#pragma warning restore FDW017
