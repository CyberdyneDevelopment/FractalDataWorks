using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="UnmatchedActions"/>, using the CRTP pattern.
/// </summary>
public abstract class UnmatchedActionBase : TypeOptionBase<UnmatchedActionBase>, IUnmatchedAction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnmatchedActionBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="displayName">The name shown to a person choosing the option.</param>
    /// <param name="description">What the option does.</param>
    /// <param name="rejectsRecord">Whether an unmatched record leaves the stream instead of passing on.</param>
    protected UnmatchedActionBase(string name, string displayName, string description, bool rejectsRecord) : base(name, displayName, description, "UnmatchedActions")
    {
        RejectsRecord = rejectsRecord;
    }

    /// <inheritdoc/>
    public bool RejectsRecord { get; }
}
