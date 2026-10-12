using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Workspace.Results;

/// <summary>
/// One reference error the ledger explained, and the reference that fixes it.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ReferenceRepair
{
    /// <summary>
    /// Gets or sets the stable id used to approve or reject this repair.
    /// </summary>
    /// <remarks>
    /// Derived from the content ("{Project}=>{RequiredAssembly}") rather than a counter, so the id a
    /// preview hands out still identifies the same repair on the apply call without the server holding
    /// session state between them.
    /// </remarks>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the project that failed to compile.</summary>
    public string Project { get; set; } = string.Empty;

    /// <summary>Gets or sets the diagnostic id (e.g. CS0246).</summary>
    public string DiagnosticId { get; set; } = string.Empty;

    /// <summary>Gets or sets the file the error occurred in.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the 1-based line the error occurred on.</summary>
    public int Line { get; set; }

    /// <summary>Gets or sets the type or namespace the compiler could not find.</summary>
    public string MissingName { get; set; } = string.Empty;

    /// <summary>Gets or sets the fully-qualified name the ledger matched it to.</summary>
    public string LedgerMatch { get; set; } = string.Empty;

    /// <summary>Gets or sets the assembly that now carries the type.</summary>
    public string RequiredAssembly { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the reference was added in memory (false on a preview).</summary>
    public bool Applied { get; set; }

    /// <summary>
    /// Gets or sets whether this repair is a "ProjectReference" or a "PackageReference".
    /// </summary>
    public string ReferenceKind { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the repair was written to the project file on disk.</summary>
    public bool WrittenToDisk { get; set; }

    /// <summary>Gets or sets what was written, or why nothing was.</summary>
    public string WriteDetail { get; set; } = string.Empty;
}
