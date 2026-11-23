using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The model turns text into speech. Data only: its contract is a later story (FDW-6262).</summary>
[TypeOption(typeof(ModelCapabilities), "Speaking")]
[ExcludeFromCodeCoverage]
public sealed class SpeakingModelCapability : ModelCapabilityBase
{
    /// <summary>Initializes a new instance of the <see cref="SpeakingModelCapability"/> class.</summary>
    public SpeakingModelCapability() : base("Speaking")
    {
    }
}
