using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Surrogate key — DB-generated identity (NEWSEQUENTIALID, IDENTITY).
/// Used for RowId and auto-increment PKs.
/// </summary>
/// <remarks>
/// Retained for backward compatibility with existing seed data. The key picker treats
/// Surrogate as equivalent to <see cref="PhysicalKeyType"/>. Use Physical for new seed entries.
/// </remarks>
// Why: EditorBrowsable(Never) hides from IntelliSense without generating CS0618; [Obsolete] is
// avoided because the TypeCollection source generator emits new SurrogateKeyType() and would
// produce a CS0618 error in Release (TreatWarningsAsErrors) from generated code we cannot annotate.
[EditorBrowsable(EditorBrowsableState.Never)]
[ExcludeFromCodeCoverage]
[TypeOption(typeof(KeyTypes), "Surrogate")]
public sealed class SurrogateKeyType : KeyTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SurrogateKeyType"/> class.</summary>
    public SurrogateKeyType()
        : base(
            id: 1,
            name: "Surrogate",
            isPrimaryKey: true,
            hasConstraint: true,
            isReference: false,
            isSystemGenerated: true)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Why: Each surrogate key value is unique per row (DB-generated sequential/random GUID).
    /// </remarks>
    public override bool SupportsUniqueness => true;
}
