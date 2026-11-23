using Fdw.Collections;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// One option of <see cref="FinishReasons"/>: why a model stopped producing a reply.
/// </summary>
public interface IFinishReason : ITypeOption<int, FinishReasonBase>
{

    /// <summary>Gets a value indicating whether the reply asks the host to run tools and call again with their results.</summary>
    bool ContinuesWithToolResults { get; }
}
