using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Workspace.Commands;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Workspace.Translators;

/// <summary>
/// Translator for the <see cref="ApplyWorkspaceChangesCommand"/>. Returns a placeholder result —
/// the real <see cref="Fdw.Workspace.Roslyn.IRoslynWorkspace.ApplyChanges(bool,System.Threading.CancellationToken)"/> call, and its result,
/// are performed by <see cref="RoslynCommandHandler"/> after this translator returns (the same
/// pattern as SetBaseline/CreateSnapshot: every <see cref="IRoslynCommandTranslator"/> is
/// instantiated via a bare <c>new()</c> at module-init time by the source-generated registration,
/// before any DI container exists, so a translator can never take a constructor-injected
/// <see cref="Fdw.Workspace.Roslyn.IRoslynWorkspace"/> — the module initializer silently skips any
/// <c>[TypeOption]</c>-tagged type without a public parameterless constructor).
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "ApplyWorkspaceChanges")]
public sealed class ApplyWorkspaceChangesTranslator
    : RoslynCommandTranslatorBase<ApplyWorkspaceChangesCommand, QueryResult<IReadOnlyList<string>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplyWorkspaceChangesTranslator"/> class.
    /// </summary>
    public ApplyWorkspaceChangesTranslator()
        : base("ApplyWorkspaceChanges", "Persists in-memory document changes accumulated by prior mutation commands to disk")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<QueryResult<IReadOnlyList<string>>>> Translate(
        ApplyWorkspaceChangesCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var placeholder = new QueryResult<IReadOnlyList<string>>(
            "Pending — applied by the command handler", System.Array.Empty<string>());

        return Task.FromResult<IGenericResult<QueryResult<IReadOnlyList<string>>>>(
            GenericResult<QueryResult<IReadOnlyList<string>>>.Success(placeholder));
    }
}
