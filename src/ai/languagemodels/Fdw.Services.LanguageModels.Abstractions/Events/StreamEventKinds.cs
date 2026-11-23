using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// The TypeCollection of the kinds of event a streamed reply is made of. A consumer's own provider package adds options in its own assembly.
/// </summary>
[TypeCollection(typeof(StreamEventKindBase), typeof(IStreamEventKind), typeof(StreamEventKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class StreamEventKinds : TypeCollectionBase<StreamEventKindBase, IStreamEventKind>
{
}
