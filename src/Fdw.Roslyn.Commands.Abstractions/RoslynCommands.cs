using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Development.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Type collection for Roslyn commands.
/// Child collection of <see cref="DevelopmentCommands"/> for C# specific commands.
/// Discovers all commands marked with [TypeOption(typeof(RoslynCommands), "CommandName", RestrictToCurrentCompilation = true)].
/// </summary>
[TypeCollection(typeof(RoslynCommandBase), typeof(IRoslynCommand), typeof(RoslynCommands),
    TypeOption = typeof(DevelopmentCommands), TypeOptionName = "Roslyn")]
public abstract partial class RoslynCommands
    : TypeCollectionBase<RoslynCommandBase, IRoslynCommand>
{
}
