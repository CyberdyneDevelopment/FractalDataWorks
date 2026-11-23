using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// Base class for the options of <see cref="StreamEventKinds"/>: the kinds of event a streamed reply is made of.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class StreamEventKindBase : TypeOptionBase<StreamEventKindBase>, IStreamEventKind
{
    /// <summary>Initializes a new instance of the <see cref="StreamEventKindBase"/> class.</summary>
    /// <param name="name">The name of the option.</param>
    /// <param name="endsStream">Whether this kind is the last event of a stream.</param>
    protected StreamEventKindBase(string name, bool endsStream) : base(name)
    {
        EndsStream = endsStream;
    }

    /// <inheritdoc />
    public bool EndsStream { get; }
}
