using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Right-aligned text.</summary>
[TypeOption(typeof(ColumnAlignments), "Right")]
[ExcludeFromCodeCoverage]
public sealed class RightColumnAlignment : ColumnAlignmentBase
{
    /// <summary>Initializes a new instance of <see cref="RightColumnAlignment"/>.</summary>
    public RightColumnAlignment() : base(3, "Right") { }
}
