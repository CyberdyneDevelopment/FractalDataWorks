using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>
/// Key-value layout - simple key-value pairs.
/// </summary>
/// <remarks>
/// <para>
/// Represents data organized as simple name-value pairs with no inherent structure.
/// </para>
/// <para>
/// Examples: Redis stores, configuration sections, environment variables.
/// </para>
/// <para>
/// Characteristics:
/// <list type="bullet">
/// <item>SupportsNesting: false - No hierarchical structures</item>
/// <item>SupportsFlattening: true - Can be flattened to a simple table</item>
/// <item>IsTabular: false - Not a native row/column format</item>
/// </list>
/// </para>
/// </remarks>
[TypeOption(typeof(DataLayouts), "KeyValue")]
[ExcludeFromCodeCoverage]
public sealed class KeyValueLayout : DataLayoutBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyValueLayout"/> class.
    /// </summary>
    public KeyValueLayout()
        : base(
            id: 4,
            name: "KeyValue",
            description: "Key-value pairs (Redis, config sections)",
            supportsNesting: false,
            supportsFlattening: true,
            isTabular: false)
    {
    }
}
