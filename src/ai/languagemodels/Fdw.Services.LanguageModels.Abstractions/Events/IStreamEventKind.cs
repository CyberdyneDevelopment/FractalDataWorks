using Fdw.Collections;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// One option of <see cref="StreamEventKinds"/>: the kinds of event a streamed reply is made of.
/// </summary>
public interface IStreamEventKind : ITypeOption<int, StreamEventKindBase>
{

    /// <summary>Gets a value indicating whether this kind is the last event of a stream.</summary>
    bool EndsStream { get; }
}
