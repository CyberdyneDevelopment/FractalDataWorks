using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions;

/// <summary>
/// Typed primitive client for RoslynWorkspace operations.
/// This is the cross-boundary surface connectors call directly per the §1.1 canary experiment.
/// </summary>
public interface IRoslynWorkspaceClient
{
    /// <summary>
    /// Retrieves the source text for the symbol identified by the given Roslyn
    /// <c>DocumentationCommentId</c> (e.g. <c>T:Foo.Bar</c> or <c>M:Foo.Bar.Baz(System.Int32)</c>).
    /// </summary>
    /// <param name="symbolId">Roslyn DocumentationCommentId for the target symbol.</param>
    /// <param name="lines">Optional line range to slice (1-based, inclusive). Null returns full span.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<RawText>> GetSymbolSource(
        string symbolId,
        RawTextLineRange? lines,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Walks all compilations in the workspace and emits a graph of projects and their
    /// dependency edges.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<WorkspaceGraph>> GetGraph(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an agent-supplied human-readable name (e.g. <c>"UserService.Save"</c>) to a single
    /// <see cref="RoslynSymbolMatch"/>. Uses <c>Type.Member</c> shape decomposition and a scoring
    /// pass over <c>SymbolFinder.FindSourceDeclarationsAsync</c> matches.
    /// </summary>
    /// <param name="name">Agent-supplied name. Empty/whitespace returns a structured failure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<RoslynSymbolMatch>> ResolveSymbol(
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds methods/properties that invoke the given symbol via
    /// <c>SymbolFinder.FindCallersAsync</c>. Result is capped to <paramref name="max"/>.
    /// </summary>
    /// <param name="symbolId">Roslyn DocumentationCommentId for the target symbol.</param>
    /// <param name="max">Maximum number of caller matches to return. Must be &gt; 0.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<IReadOnlyList<RoslynSymbolMatch>>> FindCallers(
        string symbolId,
        int max,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds methods/properties referenced from the given symbol's body — its callees plus member
    /// references discovered via syntax walk + semantic model. Returns distinct symbols capped to
    /// <paramref name="max"/>.
    /// </summary>
    /// <param name="symbolId">Roslyn DocumentationCommentId for the source symbol.</param>
    /// <param name="max">Maximum number of callee matches to return. Must be &gt; 0.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<IReadOnlyList<RoslynSymbolMatch>>> FindCallees(
        string symbolId,
        int max,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds implementations of the given interface or overrides of the given abstract/virtual
    /// member via <c>SymbolFinder.FindImplementationsAsync</c>. Capped to <paramref name="max"/>.
    /// </summary>
    /// <param name="symbolId">Roslyn DocumentationCommentId for the contract symbol.</param>
    /// <param name="max">Maximum number of matches to return. Must be &gt; 0.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<IReadOnlyList<RoslynSymbolMatch>>> FindImplementations(
        string symbolId,
        int max,
        CancellationToken cancellationToken = default);
}
