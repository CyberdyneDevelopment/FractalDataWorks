using System;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>Who a schedule's fire acts as (FDW-6144): the person its stored grant names, or no person for a schedule that has no grant.</summary>
/// <param name="PersonId">The person the run acts as; none when the schedule has no grant and its runs act as its tenant, as they always have.</param>
public sealed record ScheduledRunPerson(Guid? PersonId);
