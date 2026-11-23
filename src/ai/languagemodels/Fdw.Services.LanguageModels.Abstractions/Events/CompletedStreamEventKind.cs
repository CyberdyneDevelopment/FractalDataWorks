using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The reply is complete.</summary>
[TypeOption(typeof(StreamEventKinds), "Completed")]
[ExcludeFromCodeCoverage]
public sealed class CompletedStreamEventKind : StreamEventKindBase
{
    /// <summary>Initializes a new instance of the <see cref="CompletedStreamEventKind"/> class.</summary>
    public CompletedStreamEventKind() : base("Completed", true)
    {
    }
}
