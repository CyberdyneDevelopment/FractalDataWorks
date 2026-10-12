using Fdw.Collections;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Represents a command that operates on a Roslyn solution.
/// </summary>
public interface IRoslynCommand : ITypeOption<int, RoslynCommandBase>
{
    /// <summary>
    /// Gets the command category. May be null for Empty sentinel.
    /// </summary>
    IRoslynCommandCategory? CommandCategory { get; }
}
