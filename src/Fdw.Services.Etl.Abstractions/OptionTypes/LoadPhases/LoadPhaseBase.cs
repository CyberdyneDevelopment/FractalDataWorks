using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="LoadPhases"/>, using the CRTP pattern.
/// </summary>
public abstract class LoadPhaseBase : TypeOptionBase<LoadPhaseBase>, ILoadPhase, IReservedColumnsShape
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadPhaseBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="description">What the row writes, as the console shows it beside the phase.</param>
    /// <param name="readsRejectedRecords">Whether the row writes the records the stages rejected instead of the transformed output rows.</param>
    protected LoadPhaseBase(string name, string description, bool readsRejectedRecords) : base(name, name, description, "LoadPhases")
    {
        ReadsRejectedRecords = readsRejectedRecords;
    }

    /// <inheritdoc/>
    public bool ReadsRejectedRecords { get; }

    /// <inheritdoc/>
    public virtual IReadOnlyList<string> ReservedColumns => [];
}
