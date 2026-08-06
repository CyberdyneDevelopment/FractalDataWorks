using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// Container parameter is null.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "ContainerNull", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerNullCode : SqliteDataResultCodeBase
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
