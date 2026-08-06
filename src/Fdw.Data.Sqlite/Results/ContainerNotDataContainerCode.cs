using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// Container passed to a SQLite translator does not implement IDataContainer. SQLite query
/// translation requires structured key/field metadata only available on IDataContainer.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "ContainerNotDataContainer", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerNotDataContainerCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerNotDataContainerCode"/> class.
    /// </summary>
    public ContainerNotDataContainerCode()
        : base(21002, "ContainerNotDataContainer",
            ResultSeverities.ByName("Error"),
            "Container does not implement IDataContainer — only structured data containers are valid for SQLite translators",
            isRetryable: false)
    {
    }
}
