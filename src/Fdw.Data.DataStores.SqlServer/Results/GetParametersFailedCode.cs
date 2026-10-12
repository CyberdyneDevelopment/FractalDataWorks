using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.SqlServer.Results;

/// <summary>
/// Failed to get parameters.
/// </summary>
[TypeOption(typeof(SqlServerDataStoreResultCodes), "GetParametersFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GetParametersFailedCode : SqlServerDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetParametersFailedCode"/> class.
    /// </summary>
    public GetParametersFailedCode()
        : base(71003, "GetParametersFailed",
            ResultSeverities.ByName("Error"),
            "Failed to get parameters: {error}",
            isRetryable: true)
    {
    }
}