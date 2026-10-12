using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Workspace.Results;

/// <summary>
/// The outcome of a <c>RepairMovedReferences</c> run.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ReferenceRepairData
{
    /// <summary>Gets or sets the number of reference errors examined.</summary>
    public int ErrorsExamined { get; set; }

    /// <summary>Gets or sets the number the ledger explained.</summary>
    public int RepairedCount { get; set; }

    /// <summary>Gets or sets the number the ledger could not explain.</summary>
    public int UnresolvedCount { get; set; }

    /// <summary>Gets or sets the number of distinct project references added.</summary>
    public int ReferencesAdded { get; set; }

    /// <summary>Gets or sets whether this was a preview.</summary>
    public bool WasDryRun { get; set; }

    /// <summary>Gets or sets the errors the ledger explained.</summary>
    public IReadOnlyList<ReferenceRepair> Repairs { get; set; } = Array.Empty<ReferenceRepair>();

    /// <summary>Gets or sets the number of repairs written to project files on disk.</summary>
    public int WrittenToDiskCount { get; set; }

    /// <summary>Gets or sets the repairs that were explicitly rejected and therefore skipped.</summary>
    public IReadOnlyList<ReferenceRepair> Rejected { get; set; } = Array.Empty<ReferenceRepair>();

    /// <summary>Gets or sets the errors the ledger could not explain.</summary>
    public IReadOnlyList<UnresolvedReferenceError> Unresolved { get; set; } = Array.Empty<UnresolvedReferenceError>();
}
