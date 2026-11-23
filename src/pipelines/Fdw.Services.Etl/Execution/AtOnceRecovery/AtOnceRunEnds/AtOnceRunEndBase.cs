using Fdw.Collections;

namespace Fdw.Services.Etl.Execution.AtOnceRecovery;

/// <summary>Base of the options of <see cref="AtOnceRunEnds"/>.</summary>
public abstract class AtOnceRunEndBase : TypeOptionBase<AtOnceRunEndBase>, IAtOnceRunEnd
{
    /// <summary>Initializes a new instance of the <see cref="AtOnceRunEndBase"/> class.</summary>
    /// <param name="name">The end's name.</param>
    /// <param name="description">What the judgment came to.</param>
    /// <param name="settled">Whether the judgment is settled.</param>
    /// <param name="endedHere">Whether the run was ended here.</param>
    protected AtOnceRunEndBase(string name, string description, bool settled, bool endedHere)
        : base(name, name, description, nameof(AtOnceRunEnds))
    {
        Settled = settled;
        EndedHere = endedHere;
    }

    /// <inheritdoc />
    public bool Settled { get; }

    /// <inheritdoc />
    public bool EndedHere { get; }
}
