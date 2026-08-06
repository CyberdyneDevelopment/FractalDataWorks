using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// Container parameter is null.
/// </summary>
[TypeOption(typeof(PostgreSqlDataResultCodes), "ContainerNull", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerNullCode : PostgreSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerNullCode"/> class.
    /// </summary>
    public ContainerNullCode()
        : base(21000, "ContainerNull",
            ResultSeverities.ByName("Error"),
            "Container cannot be null",
            isRetryable: false)
    {
    }
}
