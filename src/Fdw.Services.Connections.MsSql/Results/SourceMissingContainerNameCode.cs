using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// Source configuration is missing ContainerName — cannot resolve to a MsSql table container.
/// </summary>
[TypeOption(typeof(MsSqlConnectionResultCodes), "SourceMissingContainerName", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceMissingContainerNameCode : MsSqlConnectionResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceMissingContainerNameCode"/> class.
    /// </summary>
    public SourceMissingContainerNameCode()
        : base(
            60000,
            "SourceMissingContainerName",
            ResultSeverities.ByName("Error"),
            "DataSet source configuration is missing ContainerName. Cannot resolve MsSql source without a container name.")
    {
    }
}
