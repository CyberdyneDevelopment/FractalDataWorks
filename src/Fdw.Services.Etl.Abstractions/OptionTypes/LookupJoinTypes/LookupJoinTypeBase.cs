using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for lookup join type options using the CRTP pattern.
/// </summary>
public abstract class LookupJoinTypeBase : TypeOptionBase<int, LookupJoinTypeBase>, ILookupJoinType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LookupJoinTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The join type name (e.g., "Inner", "Left").</param>
    /// <param name="failOnMissing">Whether a missing key fails the record (Inner) or leaves it null (Left).</param>
    protected LookupJoinTypeBase(int id, string name, bool failOnMissing) : base(id, name, "LookupJoinTypes")
    {
        FailOnMissing = failOnMissing;
    }

    /// <inheritdoc/>
    public bool FailOnMissing { get; }
}
