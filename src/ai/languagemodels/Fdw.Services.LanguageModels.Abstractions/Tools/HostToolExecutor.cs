using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The host runs the tool.</summary>
[TypeOption(typeof(ToolExecutors), "Host")]
[ExcludeFromCodeCoverage]
public sealed class HostToolExecutor : ToolExecutorBase
{
    /// <summary>Initializes a new instance of the <see cref="HostToolExecutor"/> class.</summary>
    public HostToolExecutor() : base("Host", true)
    {
    }
}
