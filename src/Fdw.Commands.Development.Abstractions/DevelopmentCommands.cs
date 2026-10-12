using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Development.Abstractions;

/// <summary>
/// Type collection for all development commands.
/// Language-specific commands (Roslyn, JavaScript, etc.) are child collections.
/// </summary>
[TypeCollection(typeof(DevelopmentCommandBase), typeof(IDevelopmentCommand), typeof(DevelopmentCommands))]
public abstract partial class DevelopmentCommands
    : TypeCollectionBase<DevelopmentCommandBase, IDevelopmentCommand>
{
}
