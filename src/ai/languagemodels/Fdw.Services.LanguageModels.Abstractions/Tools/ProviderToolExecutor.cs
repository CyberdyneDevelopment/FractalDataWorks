using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The provider runs the tool itself.</summary>
[TypeOption(typeof(ToolExecutors), "Provider")]
[ExcludeFromCodeCoverage]
public sealed class ProviderToolExecutor : ToolExecutorBase
{
    /// <summary>Initializes a new instance of the <see cref="ProviderToolExecutor"/> class.</summary>
    public ProviderToolExecutor() : base("Provider", false)
    {
    }
}
