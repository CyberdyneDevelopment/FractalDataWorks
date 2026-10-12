using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// A variable a Notify message may use in double braces, such as <c>{{RowsLoaded}}</c>, and the way it reads its value from
/// a run (FDW-1120). The options of <see cref="NotifyVariables"/> are the one list of what a message may say: a message that
/// names anything else is refused, and the console offers exactly these.
/// </summary>
public interface INotifyVariable : ITypeOption<int, INotifyVariable>
{
    /// <summary>Reads this variable's value for a run.</summary>
    /// <param name="facts">What the run can say about itself now.</param>
    /// <returns>The text that replaces the variable in the message.</returns>
    string ValueOf(INotifyRunFacts facts);
}
