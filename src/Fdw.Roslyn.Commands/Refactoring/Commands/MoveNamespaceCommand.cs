using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Refactoring.Commands;

/// <summary>
/// Command to rename a namespace and every reference to it across the solution.
/// </summary>
[TypeOption(typeof(RoslynCommands), "MoveNamespace")]
public sealed class MoveNamespaceCommand : RoslynCommandBase, IReasonedCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoveNamespaceCommand"/> class.
    /// </summary>
    public MoveNamespaceCommand()
        : base("MoveNamespace", RoslynCommandCategories.Refactoring, "Rename a namespace and rewrite every reference to it solution-wide — the namespace declaration, using directives, qualified names and XML doc crefs. Use when the type is in the RIGHT place but under the WRONG name; use MoveTypeToProject for the opposite. This is CONSUMER-BREAKING: the fully-qualified name changes, so any [TypeOption] Id derived from it (FNV-1a of the FQN) changes too. Refuses to run against a workspace loaded without its test projects, because excluding them would make the rewrite incomplete by construction. DryRun defaults true and never enters the change ledger. Returns the affected files, the FQN change and whether TypeOption Ids shifted.")
    {
    }

    /// <summary>
    /// Gets or sets the namespace to rename.
    /// </summary>
    public string OldNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the replacement namespace.
    /// </summary>
    public string NewNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to preview only. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// A preview returns a query result, so it can never reach the handler's mutation branch and can
    /// never be recorded in the change ledger as work that happened.
    /// </remarks>
    public bool DryRun { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to proceed when the probe reports problems it may not be
    /// able to see the truth about. Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// For the case where the CALLER can see something the probe cannot: a reference the workspace never
    /// resolved, a generator's output, a project outside the loaded solution. The probe compiles what it
    /// was given, so "this would not compile" is only ever a statement about the loaded solution — and a
    /// solution that is missing a reference will say a change breaks things when it does not.
    ///
    /// It is defaulted OFF, so nothing changes for a caller who does not set it, and the check still runs
    /// and still reports everything it found — the override only decides whether those findings STOP the
    /// change. Setting it is itself the deliberate choice, which is what separates this from a silent
    /// fallback; deleting the check instead would let a genuinely broken rewrite land with nothing said.
    /// Supply Reason as well when you want the justification recorded in the change ledger.
    /// </remarks>
    public bool AcceptUnverified { get; set; }

    /// <summary>
    /// Gets or sets why this change is being made, recorded verbatim into the change ledger.
    /// </summary>
    /// <remarks>
    /// The ledger records WHAT moved; without this it never records WHY. A migration guide that says a
    /// type changed package is useful; one that also says which slice or issue caused it is auditable.
    /// </remarks>
    public string? Reason { get; set; }
}
