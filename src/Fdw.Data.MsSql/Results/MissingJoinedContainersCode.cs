using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Compound query missing joined containers metadata.
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "MissingJoinedContainers", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MissingJoinedContainersCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingJoinedContainersCode"/> class.
    /// </summary>
    public MissingJoinedContainersCode()
        : base(21002, "MissingJoinedContainers",
            ResultSeverities.ByName("Error"),
            "CompoundQueryCommand must have JoinedContainers in metadata",
            isRetryable: false)
    {
    }
}