using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// DataPath was not found in the specified DataStore — the store tree built from this host's
/// configuration schema registers no path by that name.
/// </summary>
/// <remarks>
/// Why this lives in Fdw.Data.Abstractions rather than Fdw.Services.Data: the node lookup that PRODUCES
/// this outcome (<c>DataStore.Path</c>) sits in Fdw.Data.DataNodes, which cannot reference the
/// Fdw.Services.Data implementation project. While the code lived there it was unreachable from the
/// producer and was consequently attached to nothing — the miss surfaced only as message text, so a
/// caller could not tell "this store has no such path" from "the load failed" without matching strings.
/// Same reason <see cref="DataStoresResultCodeBase"/> itself lives here.
/// <para>
/// Meaning for a caller: the absence is a STRUCTURAL property of this host's configuration schema, not a
/// transient fault — it cannot change for the life of the process, because the store tree is built once
/// through a <c>Lazy</c> over <c>configurationSchema.json</c>. A caller that polls should STOP on this
/// code rather than retry, and must not report it at Error.
/// </para>
/// </remarks>
[TypeOption(typeof(DataStoresResultCodes), "DataPathNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataPathNotFoundCode : DataStoresResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataPathNotFoundCode"/> class.
    /// </summary>
    public DataPathNotFoundCode()
        : base(31004, "DataPathNotFound", ResultSeverities.ByName("Error"),
            "DataPath '{PathName}' not found in DataStore '{DataStoreName}'",
            isRetryable: false)
    {
    }
}
