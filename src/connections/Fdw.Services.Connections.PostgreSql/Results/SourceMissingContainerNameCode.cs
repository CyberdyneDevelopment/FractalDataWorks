using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.PostgreSql.Results;

/// <summary>
/// Source configuration is missing ContainerName — cannot resolve to a PostgreSQL table container.
/// </summary>
[TypeOption(typeof(PostgreSqlResultCodes), "SourceMissingContainerName", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceMissingContainerNameCode : PostgreSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceMissingContainerNameCode"/> class.
    /// </summary>
    public SourceMissingContainerNameCode()
        : base(
            60000,
            "SourceMissingContainerName",
            ResultSeverities.ByName("Error"),
            "DataSet source configuration is missing ContainerName. Cannot resolve PostgreSQL source without a container name.")
    {
    }
}
