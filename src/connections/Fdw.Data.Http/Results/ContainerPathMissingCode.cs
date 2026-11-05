using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Http.Results;

/// <summary>
/// The container declares no path: a REST request is addressed by its container's path, and the container's name is a
/// label, never an address (FDW-3580: the name used to stand in for it).
/// </summary>
[TypeOption(typeof(DataHttpResultCodes), "ContainerPathMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerPathMissingCode : DataHttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerPathMissingCode"/> class.
    /// </summary>
    public ContainerPathMissingCode()
        : base(90003, "ContainerPathMissing",
            ResultSeverities.ByName("Error"),
            "Container '{ContainerName}' declares no path; a REST request is addressed by its container's path",
            isRetryable: false)
    {
    }
}
