using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.SqlServer.Results;

/// <summary>
/// Failed to create configuration writer.
/// </summary>
[TypeOption(typeof(SqlServerDataStoreResultCodes), "WriterCreationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WriterCreationFailedCode : SqlServerDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WriterCreationFailedCode"/> class.
    /// </summary>
    public WriterCreationFailedCode()
        : base(91000, "WriterCreationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to create {writerType} writer: {error}",
            isRetryable: false)
    {
    }
}