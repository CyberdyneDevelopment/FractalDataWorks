using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Interface for key type options that define how a field participates in key relationships.
/// </summary>
/// <remarks>
/// Why: Replaces the string-based KeyType column and the IsPrimaryKey flag on fields.
/// Each key type carries behavior that translators and schema tools can use:
/// <list type="bullet">
/// <item><description>Surrogate: DB-generated identity (NEWSEQUENTIALID, IDENTITY)</description></item>
/// <item><description>Natural: Business key (unique constraint, used for lookups)</description></item>
/// <item><description>Foreign: FK relationship to parent container/dataset</description></item>
/// <item><description>Join: Cross-source join key (no FK constraint, used for dataset joins)</description></item>
/// </list>
/// </remarks>
public interface IKeyType : ITypeOption<int, KeyTypeBase>
{
    /// <summary>
    /// Whether this key type represents a primary key (Surrogate or Natural).
    /// Translators use this to determine WHERE clause fields for UPDATE/DELETE.
    /// </summary>
    bool IsPrimaryKey { get; }

    /// <summary>
    /// Whether this key type implies a DB constraint (PK, UNIQUE, FK).
    /// Join keys have no constraint — they're logical relationships only.
    /// </summary>
    bool HasConstraint { get; }

    /// <summary>
    /// Whether this key type references a field on another container/dataset.
    /// True for Foreign and Join keys.
    /// </summary>
    bool IsReference { get; }

    /// <summary>
    /// Whether the key value is system-generated (IDENTITY, NEWSEQUENTIALID).
    /// Only Surrogate keys are system-generated.
    /// </summary>
    bool IsSystemGenerated { get; }
}
