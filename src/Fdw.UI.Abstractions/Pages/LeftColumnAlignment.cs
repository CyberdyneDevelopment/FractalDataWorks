using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Left-aligned text.</summary>
[TypeOption(typeof(ColumnAlignments), "Left")]
[ExcludeFromCodeCoverage]
public sealed class LeftColumnAlignment : ColumnAlignmentBase
{
    /// <summary>Initializes a new instance of <see cref="LeftColumnAlignment"/>.</summary>
    public LeftColumnAlignment() : base(1, "Left") { }
}
