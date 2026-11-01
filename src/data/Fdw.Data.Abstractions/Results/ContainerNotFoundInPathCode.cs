using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// Container was not found in the specified path within a DataStore — the path exists in this host's
/// store tree but registers no container by that name.
/// </summary>
/// <remarks>
/// Why this lives in Fdw.Data.Abstractions rather than Fdw.Services.Data: see
/// <see cref="DataPathNotFoundCode"/> — the producing node lookup (<c>DataPath.Container</c>) is in
/// Fdw.Data.DataNodes and cannot reference the Fdw.Services.Data implementation project. The same
/// structural-not-transient meaning applies: a polling caller should STOP on this code, not retry it.
/// </remarks>
[TypeOption(typeof(DataStoresResultCodes), "ContainerNotFoundInPath", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerNotFoundInPathCode : DataStoresResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerNotFoundInPathCode"/> class.
    /// </summary>
    public ContainerNotFoundInPathCode()
        : base(31002, "ContainerNotFoundInPath", ResultSeverities.ByName("Error"),
            "Container '{ContainerName}' not found in path '{PathName}' of DataStore '{DataStoreName}'",
            isRetryable: false)
    {
    }
}
