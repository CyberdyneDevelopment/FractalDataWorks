using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The reply failed.</summary>
[TypeOption(typeof(StreamEventKinds), "Failed")]
[ExcludeFromCodeCoverage]
public sealed class FailedStreamEventKind : StreamEventKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FailedStreamEventKind"/> class.</summary>
    public FailedStreamEventKind() : base("Failed", true)
    {
    }
}
