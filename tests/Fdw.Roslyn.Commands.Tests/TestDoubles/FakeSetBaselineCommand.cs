using Fdw.Roslyn.Commands.Abstractions;

namespace Fdw.Roslyn.Commands.Tests.TestDoubles;

/// <summary>
/// A command that declares it advances the workspace baseline.
/// </summary>
/// <remarks>
/// Previously it only set <c>Name = "SetBaseline"</c>, because the handler dispatched on that string.
/// Declaring the capability is the point of the change: a rename can no longer silently disable the
/// effect, and the double can no longer claim an effect it does not declare.
/// </remarks>
public sealed class FakeSetBaselineCommand : FakeRoslynCommand, IBaselineSettingCommand
{
    public FakeSetBaselineCommand()
    {
        Name = "SetBaseline";
    }
}
