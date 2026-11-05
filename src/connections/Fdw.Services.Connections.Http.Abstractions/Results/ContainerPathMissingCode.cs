using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// An Http container declares no path: a request is addressed by its container's path, and the container's name is a label, never an address
/// (FDW-3580: the name used to stand in for it).
/// </summary>
[TypeOption(typeof(HttpResultCodes), "ContainerPathMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerPathMissingCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerPathMissingCode"/> class.
    /// </summary>
    public ContainerPathMissingCode()
        : base(90006, "ContainerPathMissing",
            ResultSeverities.ByName("Error"),
            "Container '{ContainerName}' declares no path; an Http request is addressed by its container's path",
            isRetryable: false)
    {
    }
}
