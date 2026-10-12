using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Partial success - batch operation where some items succeeded and some failed.
/// </summary>
[TypeOption(typeof(ResultStatuses), "PartialSuccess", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PartialSuccessStatus : ResultStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PartialSuccessStatus"/> class.
    /// </summary>
    public PartialSuccessStatus()
        : base(
            id: 3,
            name: "PartialSuccess",
            isSuccess: true,
            requiresAttention: true)
    {
    }
}
