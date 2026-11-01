using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Success status - clean success, no issues.
/// </summary>
[TypeOption(typeof(ResultStatuses), "Success", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SuccessStatus : ResultStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SuccessStatus"/> class.
    /// </summary>
    public SuccessStatus()
        : base(
            id: 0,
            name: "Success",
            isSuccess: true,
            requiresAttention: false)
    {
    }
}
