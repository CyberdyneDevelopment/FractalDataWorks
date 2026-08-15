using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>A field a dataset may select.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FieldVisibilities), "Visible", RestrictToCurrentCompilation = true)]
public sealed class VisibleField : FieldVisibilityBase
{
    /// <summary>Initializes a new instance of the <see cref="VisibleField"/> class.</summary>
    public VisibleField()
        : base(id: 1, name: "Visible", allowsProjection: true)
    {
    }
}
