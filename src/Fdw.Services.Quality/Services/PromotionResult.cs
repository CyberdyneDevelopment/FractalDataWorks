using System;
using System.Collections.Generic;

namespace Fdw.Services.Quality.Services;

/// <summary>
/// Result of promotion execution.
/// </summary>
public sealed record PromotionResult(
    Guid RequestId,
    string SourceEnvironment,
    string TargetEnvironment,
    int TotalItems,
    int SuccessfulItems,
    int FailedItems,
    DateTimeOffset CompletedAt,
    IReadOnlyList<PromotionItemResult> Items);