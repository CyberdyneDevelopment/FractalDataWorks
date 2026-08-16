using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// A column whose permitted values come from another container.
/// </summary>
/// <remarks>
/// <para>
/// Distinct from <c>Foreign</c>, which means the row belongs to a parent and is meaningless without
/// it — a typed body under its header, or a child under its owner. A ValuesFrom column cites a lookup
/// and the row stands on its own: <c>data.DataSet.CategoryRowId</c> names a category, and a dataset
/// with no category is still a dataset.
/// </para>
/// <para>
/// Both are foreign keys in SQL, which is why the distinction has to be declared rather than inferred.
/// The reader treats a Foreign key as a parent and will resolve the row only through it — so a root
/// configuration that merely cites a lookup becomes unreadable by its own name the moment someone adds
/// the constraint. Declaring the citation as ValuesFrom keeps referential integrity in the database
/// without changing how the row is read.
/// </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[ExcludeFromCodeCoverage]
[TypeOption(typeof(KeyTypes), "ValuesFrom")]
public sealed class ValuesFromKeyType : KeyTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ValuesFromKeyType"/> class.</summary>
    public ValuesFromKeyType()
        : base(
            id: 8,
            name: "ValuesFrom",
            isPrimaryKey: false,
            hasConstraint: true,
            isReference: true,
            isSystemGenerated: false)
    {
    }
}
