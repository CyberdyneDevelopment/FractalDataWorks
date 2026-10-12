using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// BatchCopy/Streaming pipeline executed without a configured SourceDataSet — the extract
/// phase has nothing to read from. Caller must bind a source before executing.
/// </summary>
[TypeOption(typeof(EtlResultCodes), "SourceDataSetRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceDataSetRequiredCode : EtlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceDataSetRequiredCode"/> class.
    /// </summary>
    public SourceDataSetRequiredCode()
        : base(20000, "SourceDataSetRequired",
            ResultSeverities.ByName("Error"),
            "Pipeline '{PipelineName}' has no SourceDataSet bound; configure a source before executing.",
            isRetryable: false)
    {
    }
}
