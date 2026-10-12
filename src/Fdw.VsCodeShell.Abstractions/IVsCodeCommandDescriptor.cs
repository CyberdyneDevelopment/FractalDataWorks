namespace Fdw.VsCodeShell.Abstractions;

/// <summary>
/// Declares a command the bootstrap should register via <c>vscode.commands.registerCommand</c>.
/// Invocation is forwarded to the .NET host as <c>POST /vscode/commands/{Id}</c>.
/// </summary>
public interface IVsCodeCommandDescriptor
{
    /// <summary>Command id (e.g. <c>pidgin.openCanvas</c>).</summary>
    string Id { get; }

    /// <summary>Title shown in the VS Code command palette.</summary>
    string Title { get; }

    /// <summary>Optional category prefix in the palette.</summary>
    string? Category { get; }

    /// <summary>Editor context the command needs: <c>none</c>, <c>cursor</c>, <c>selection</c>, <c>document</c>.</summary>
    string ContextKind { get; }
}
