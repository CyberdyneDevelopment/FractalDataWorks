using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="RunWaitReasons"/>, using the CRTP pattern.
/// </summary>
public abstract class RunWaitReasonBase : TypeOptionBase<RunWaitReasonBase>, IRunWaitReason
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RunWaitReasonBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="description">What the wait means, as the console shows it beside the run.</param>
    protected RunWaitReasonBase(string name, string description) : base(name, name, description, "RunWaitReasons")
    {
    }
}
