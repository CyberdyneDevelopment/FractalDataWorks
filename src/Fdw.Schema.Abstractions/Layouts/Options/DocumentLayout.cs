using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>
/// Document layout - single complex object.
/// </summary>
/// <remarks>
/// <para>
/// Represents data organized as a self-contained document with nested fields.
/// </para>
/// <para>
/// Examples: MongoDB documents, JSON configuration files, NoSQL records.
/// </para>
/// <para>
/// Characteristics:
/// <list type="bullet">
/// <item>SupportsNesting: true - Can contain nested structures</item>
/// <item>SupportsFlattening: true - Can be flattened with path expressions</item>
/// <item>IsTabular: false - Not a native row/column format</item>
/// </list>
/// </para>
/// </remarks>
[TypeOption(typeof(DataLayouts), "Document")]
[ExcludeFromCodeCoverage]
public sealed class DocumentLayout : DataLayoutBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentLayout"/> class.
    /// </summary>
    public DocumentLayout()
        : base(
            id: 3,
            name: "Document",
            description: "Single complex object (MongoDB document, config)",
            supportsNesting: true,
            supportsFlattening: true,
            isTabular: false)
    {
    }
}
