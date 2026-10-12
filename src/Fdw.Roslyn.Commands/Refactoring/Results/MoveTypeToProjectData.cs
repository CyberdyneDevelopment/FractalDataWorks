using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>
/// The outcome of a <c>MoveTypeToProject</c> run.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MoveTypeToProjectData
{
    /// <summary>Gets or sets the namespace whose types were moved.</summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>Gets or sets the project the documents came from.</summary>
    public string SourceProject { get; set; } = string.Empty;

    /// <summary>Gets or sets the project the documents moved to.</summary>
    public string TargetProject { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of documents moved.</summary>
    public int DocumentsMoved { get; set; }

    /// <summary>Gets or sets whether this was a preview.</summary>
    public bool WasDryRun { get; set; }

    /// <summary>
    /// Gets or sets the consumer-impact statement. A move preserves the fully-qualified name, so it is
    /// NOT consumer-breaking in the way a namespace rename is.
    /// </summary>
    public string ConsumerImpact { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets what the TARGET project must reference for the moved documents to compile, derived by
    /// resolving every referenced symbol to its containing assembly.
    /// </summary>
    public IReadOnlyList<ReferenceRequirement> RequiredReferences { get; set; } = Array.Empty<ReferenceRequirement>();

    /// <summary>
    /// Gets or sets what was actually written into the target project's csproj.
    /// </summary>
    /// <remarks>
    /// Deliberately separate from <see cref="RequiredReferences"/>: that is what the symbol graph says is
    /// NEEDED, this is what landed on disk. They silently diverged before — the closure was computed,
    /// reported, wired in memory, and never written, because a ProjectReference is project metadata and
    /// the persistence path writes documents. Reporting both makes any future gap visible.
    /// </remarks>
    public IReadOnlyList<string> ReferencesWritten { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets the source generators the moved code needs referenced as analyzers.</summary>
    public IReadOnlyList<GeneratorRequirement> RequiredGenerators { get; set; } = Array.Empty<GeneratorRequirement>();

    /// <summary>Gets or sets references the target cannot legally take because of its target framework.</summary>
    public IReadOnlyList<string> IncompatibleReferences { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets what the SOURCE project can now shed. This is the payoff metric: it answers whether
    /// the move bought anything. Zero is reported honestly rather than hidden.
    /// </summary>
    public IReadOnlyList<string> DroppableReferences { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets everything that will break because of this move.</summary>
    public IReadOnlyList<BreakFinding> Breaks { get; set; } = Array.Empty<BreakFinding>();

    /// <summary>Gets or sets the moved document paths, old to new.</summary>
    public IReadOnlyList<string> MovedFiles { get; set; } = Array.Empty<string>();
}
