using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>Something a tool the provider runs itself did, or its result.</summary>
[TypeOption(typeof(StreamEventKinds), "HostedToolActivity")]
[ExcludeFromCodeCoverage]
public sealed class HostedToolActivityStreamEventKind : StreamEventKindBase
{
    /// <summary>Initializes a new instance of the <see cref="HostedToolActivityStreamEventKind"/> class.</summary>
    public HostedToolActivityStreamEventKind() : base("HostedToolActivity", false)
    {
    }
}
