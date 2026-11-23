using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// The TypeCollection of how much reasoning a model does before it replies, stated per call. A consumer's own provider package adds options in its own assembly.
/// </summary>
[TypeCollection(typeof(ReasoningEffortBase), typeof(IReasoningEffort), typeof(ReasoningEfforts))]
[ExcludeFromCodeCoverage]
public abstract partial class ReasoningEfforts : TypeCollectionBase<ReasoningEffortBase, IReasoningEffort>
{
}
