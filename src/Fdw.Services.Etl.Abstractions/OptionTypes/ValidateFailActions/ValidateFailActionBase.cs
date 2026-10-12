using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="ValidateFailActions"/>, using the CRTP pattern.
/// </summary>
public abstract class ValidateFailActionBase : TypeOptionBase<ValidateFailActionBase>, IValidateFailAction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateFailActionBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="displayName">The name shown to a person choosing the option.</param>
    /// <param name="description">What the option does.</param>
    /// <param name="stopsRun">Whether a failing record stops the run instead of leaving the stream.</param>
    protected ValidateFailActionBase(string name, string displayName, string description, bool stopsRun) : base(name, displayName, description, "ValidateFailActions")
    {
        StopsRun = stopsRun;
    }

    /// <inheritdoc/>
    public bool StopsRun { get; }
}
