using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// Details of ProjectReference changes needed in a single .csproj file.
/// </summary>
public sealed class CsprojChangeDetail
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsprojChangeDetail"/> class.
    /// </summary>
    public CsprojChangeDetail(string csprojPath, IReadOnlyList<ReferencePathChange> referenceChanges)
    {
        CsprojPath = csprojPath ?? throw new ArgumentNullException(nameof(csprojPath));
        ReferenceChanges = referenceChanges ?? throw new ArgumentNullException(nameof(referenceChanges));
    }

    /// <summary>
    /// Gets the absolute path to the .csproj file.
    /// </summary>
    public string CsprojPath { get; }

    /// <summary>
    /// Gets the list of ProjectReference Include path changes.
    /// </summary>
    public IReadOnlyList<ReferencePathChange> ReferenceChanges { get; }
}
