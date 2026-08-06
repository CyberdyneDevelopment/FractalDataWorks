using System;
using Fdw.ServiceTypes;

namespace Fdw.VsCodeShell.Abstractions;

/// <summary>
/// Non-generic marker for a VS Code command declared as a <c>[ServiceTypeOption]</c>.
/// The collection is keyed on this interface, and the manifest is projected from it.
/// </summary>
/// <remarks>
/// Every member here is also read by the generated lookup code, so each one must stay on the
/// interface rather than only on <see cref="VsCodeCommandTypeBase{THandler}"/> — the generator emits
/// <c>ToFrozenDictionary(x =&gt; x.CommandId)</c> against this interface type.
/// </remarks>
public interface IVsCodeCommandType : IServiceType
{
    /// <summary>
    /// The id VS Code invokes and the bootstrap POSTs to <c>/vscode/commands/{id}</c> — e.g. <c>pidgin.openCanvas</c>.
    /// </summary>
    /// <remarks>
    /// Distinct from <c>Name</c>. <c>Name</c> is the <c>[ServiceTypeOption]</c> option name and must be a valid
    /// C# identifier, which a dotted command id is not. The two are separate keyspaces with separate lookups.
    /// </remarks>
    string CommandId { get; }

    /// <summary>Palette title, without the category prefix — VS Code renders it as <c>Category: Title</c>.</summary>
    string Title { get; }

    /// <summary>
    /// Optional VS Code palette grouping (e.g. <c>Pidgin</c>).
    /// </summary>
    /// <remarks>
    /// Named <c>PaletteCategory</c>, not <c>Category</c>, because <c>TypeOptionBase.Category</c> already exists
    /// and means something else — the FDW service category (<c>VsCodeCommand</c>) that groups this option
    /// within the framework. Reusing the name would hide the base member and silently conflate the two.
    /// </remarks>
    string? PaletteCategory { get; }

    /// <summary>How much editor context the bootstrap captures before dispatch: none, cursor, selection, or document.</summary>
    string ContextKind { get; }

    /// <summary>The webview this command opens, or null when it opens none.</summary>
    VsCodeWebview? Webview { get; }

    /// <summary>The concrete handler type this command dispatches to.</summary>
    Type HandlerType { get; }
}
