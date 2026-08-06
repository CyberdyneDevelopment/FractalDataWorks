using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Success with warnings - succeeded but warnings should be surfaced.
/// </summary>
[TypeOption(typeof(ResultStatuses), "SuccessWithWarnings", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SuccessWithWarningsStatus : ResultStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SuccessWithWarningsStatus"/> class.
    /// </summary>
    public SuccessWithWarningsStatus()
        : base(
            id: 1,
            name: "SuccessWithWarnings",
            isSuccess: true,
            requiresAttention: true)
    {
    }
}
