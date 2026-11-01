using System;
using System.Collections.Generic;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// What one run of the retention job found and did for an organization (FDW-1575).
/// </summary>
/// <param name="RanAt">The moment the job judged every document against.</param>
/// <param name="Considered">How many documents of the types with a policy the job looked at.</param>
/// <param name="Held">How many were past their deadline but under a legal hold, so were left alone.</param>
/// <param name="Undated">How many lack the date their policy counts from, so have no deadline.</param>
/// <param name="Ended">The documents the job ended.</param>
/// <param name="Failed">The documents the job could not end, each with the reason.</param>
public sealed record RetentionRunReport(
    DateTimeOffset RanAt, int Considered, int Held, int Undated, IReadOnlyList<RetentionOutcome> Ended, IReadOnlyList<RetentionOutcome> Failed);
