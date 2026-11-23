using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A planner asked the data gateway to describe a container for pushing a read into its store, and the gateway does not offer that for it
/// (FDW-6969): the store is one the host declares, whose reads go through the configuration gateway.
/// </summary>
/// <remarks>
/// The planner leaves every read of that container in memory and says why in its trace; nothing is read differently. A non-error outcome.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "PushdownNotOffered", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PushdownNotOfferedCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="PushdownNotOfferedCode"/> class.</summary>
    public PushdownNotOfferedCode()
        : base(10970, "PushdownNotOffered",
            ResultSeverities.ByName("Information"),
            "Container '{container}' of data store '{dataStore}' is not described for pushing a read into its store: {reason}",
            isRetryable: false)
    {
    }
}
