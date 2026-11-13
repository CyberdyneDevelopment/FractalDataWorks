using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A component of a Graph data set's graph states a setting that only a pipeline's component takes.
/// </summary>
/// <remarks>Refused when the graph is saved (FDW-5287). HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetSettingNotSupported", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetSettingNotSupportedCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetSettingNotSupportedCode"/> class.</summary>
    public GraphDataSetSettingNotSupportedCode()
        : base(21033, "GraphDataSetSettingNotSupported",
            ResultSeverities.ByName("Error"),
            "Component '{component}' of Graph data set '{dataSet}' states the setting '{setting}', which a Graph data set's component does not take",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
