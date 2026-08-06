using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Center-aligned text.</summary>
[TypeOption(typeof(ColumnAlignments), "Center")]
[ExcludeFromCodeCoverage]
public sealed class CenterColumnAlignment : ColumnAlignmentBase
{
    /// <summary>Initializes a new instance of <see cref="CenterColumnAlignment"/>.</summary>
    public CenterColumnAlignment() : base(2, "Center") { }
}
