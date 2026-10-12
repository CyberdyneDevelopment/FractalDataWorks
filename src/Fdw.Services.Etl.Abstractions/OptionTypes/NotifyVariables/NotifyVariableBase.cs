using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="NotifyVariables"/>, using the CRTP pattern.
/// </summary>
public abstract class NotifyVariableBase : TypeOptionBase<NotifyVariableBase>, INotifyVariable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyVariableBase"/> class.
    /// </summary>
    /// <param name="name">The option name, which is the variable's name without braces.</param>
    /// <param name="displayName">The name shown to a person choosing the variable.</param>
    /// <param name="description">What the variable stands for.</param>
    protected NotifyVariableBase(string name, string displayName, string description) : base(name, displayName, description, "NotifyVariables")
    {
    }

    /// <inheritdoc/>
    public abstract string ValueOf(INotifyRunFacts facts);
}
