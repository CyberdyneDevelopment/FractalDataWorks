using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The model turns speech into text. Data only: its contract is a later story (FDW-6262).</summary>
[TypeOption(typeof(ModelCapabilities), "Transcription")]
[ExcludeFromCodeCoverage]
public sealed class TranscriptionModelCapability : ModelCapabilityBase
{
    /// <summary>Initializes a new instance of the <see cref="TranscriptionModelCapability"/> class.</summary>
    public TranscriptionModelCapability() : base("Transcription")
    {
    }
}
