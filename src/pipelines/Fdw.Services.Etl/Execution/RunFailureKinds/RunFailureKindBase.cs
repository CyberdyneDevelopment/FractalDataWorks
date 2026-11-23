using Fdw.Collections;

namespace Fdw.Services.Etl.Execution;

/// <summary>Base class for the kinds of failure that ended a pipeline run.</summary>
public abstract class RunFailureKindBase : TypeOptionBase<RunFailureKindBase>, IRunFailureKind
{
    /// <summary>Initializes a new instance of the <see cref="RunFailureKindBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="classified">Whether a failure of this kind carries a coded failure message in a known category.</param>
    /// <param name="refused">Whether a failure of this kind does not count toward a failure threshold.</param>
    protected RunFailureKindBase(string name, bool classified, bool refused)
        : base(name)
    {
        Classified = classified;
        Refused = refused;
    }

    /// <inheritdoc />
    public bool Classified { get; }

    /// <inheritdoc />
    public bool Refused { get; }

    /// <inheritdoc />
    public abstract string Describe(RunFailureClassification failure);
}
