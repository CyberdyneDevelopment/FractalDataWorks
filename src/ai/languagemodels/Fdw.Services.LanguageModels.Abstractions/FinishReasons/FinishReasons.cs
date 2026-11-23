using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// The TypeCollection of why a model stopped producing a reply. A consumer's own provider package adds options in its own assembly.
/// </summary>
[TypeCollection(typeof(FinishReasonBase), typeof(IFinishReason), typeof(FinishReasons))]
[ExcludeFromCodeCoverage]
public abstract partial class FinishReasons : TypeCollectionBase<FinishReasonBase, IFinishReason>
{
}
