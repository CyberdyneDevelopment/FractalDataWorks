using Fdw.Collections;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// One option of <see cref="ReasoningEfforts"/>: how much reasoning a model does before it replies, stated per call.
/// </summary>
public interface IReasoningEffort : ITypeOption<int, ReasoningEffortBase>
{
}
