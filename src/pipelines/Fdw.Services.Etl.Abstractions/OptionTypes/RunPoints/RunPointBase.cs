using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="RunPoints"/>, using the CRTP pattern.
/// </summary>
public abstract class RunPointBase : TypeOptionBase<RunPointBase>, IRunPoint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RunPointBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="displayName">The name shown to a person choosing the option.</param>
    /// <param name="description">What the option does.</param>
    /// <param name="firesAfterStage">Whether the notification fires after the final page has passed the stage.</param>
    /// <param name="firesAfterLoad">Whether the notification fires after the load has committed.</param>
    /// <param name="firesOnFailure">Whether the notification fires when the run fails.</param>
    protected RunPointBase(string name, string displayName, string description, bool firesAfterStage, bool firesAfterLoad, bool firesOnFailure) : base(name, displayName, description, "RunPoints")
    {
        FiresAfterStage = firesAfterStage;
        FiresAfterLoad = firesAfterLoad;
        FiresOnFailure = firesOnFailure;
    }

    /// <inheritdoc/>
    public bool FiresAfterStage { get; }

    /// <inheritdoc/>
    public bool FiresAfterLoad { get; }

    /// <inheritdoc/>
    public bool FiresOnFailure { get; }
}
