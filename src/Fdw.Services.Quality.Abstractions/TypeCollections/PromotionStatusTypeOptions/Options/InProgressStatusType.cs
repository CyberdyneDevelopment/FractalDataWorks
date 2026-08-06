using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.PromotionStatusTypeOptions.Options;

/// <summary>
/// InProgress status type indicating a promotion is currently executing.
/// </summary>
[TypeOption(typeof(PromotionStatusTypes), "InProgress")]
[ExcludeFromCodeCoverage]
public sealed class InProgressStatusType : PromotionStatusTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InProgressStatusType"/> class.
    /// </summary>
    public InProgressStatusType()
        : base(
            id: 4,
            name: "InProgress",
            isTerminal: false,
            isSuccess: false,
            allowsExecution: false)
    {
    }
}
