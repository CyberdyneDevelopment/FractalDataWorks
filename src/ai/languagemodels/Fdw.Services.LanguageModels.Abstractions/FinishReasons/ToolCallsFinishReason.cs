using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The reply asks the host to run tools.</summary>
[TypeOption(typeof(FinishReasons), "ToolCalls")]
[ExcludeFromCodeCoverage]
public sealed class ToolCallsFinishReason : FinishReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="ToolCallsFinishReason"/> class.</summary>
    public ToolCallsFinishReason() : base("ToolCalls", true)
    {
    }
}
