using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// Base class for the options of <see cref="FinishReasons"/>: why a model stopped producing a reply.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class FinishReasonBase : TypeOptionBase<FinishReasonBase>, IFinishReason
{
    /// <summary>Initializes a new instance of the <see cref="FinishReasonBase"/> class.</summary>
    /// <param name="name">The name of the option.</param>
    /// <param name="continuesWithToolResults">Whether the reply asks the host to run tools and call again with their results.</param>
    protected FinishReasonBase(string name, bool continuesWithToolResults) : base(name)
    {
        ContinuesWithToolResults = continuesWithToolResults;
    }

    /// <inheritdoc />
    public bool ContinuesWithToolResults { get; }
}
