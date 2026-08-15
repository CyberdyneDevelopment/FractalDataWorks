using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// A field the container declares but no dataset may select.
/// </summary>
/// <remarks>
/// The physical key is the case this exists for. A key definition names the field it is built on, so
/// the field has to be declared — but returning it would put a storage detail in a dataset, and the
/// whole point of the container abstraction is that the caller never sees one. An admin or analyst
/// authoring a container still reads the full field list; only projection is refused.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FieldVisibilities), "NotVisible", RestrictToCurrentCompilation = true)]
public sealed class NotVisibleField : FieldVisibilityBase
{
    /// <summary>Initializes a new instance of the <see cref="NotVisibleField"/> class.</summary>
    public NotVisibleField()
        : base(id: 2, name: "NotVisible", allowsProjection: false)
    {
    }
}
