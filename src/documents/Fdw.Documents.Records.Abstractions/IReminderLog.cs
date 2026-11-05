using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>Reads and records the reminders sent about an organization's unsatisfied requirements (FDW-3395).</summary>
/// <remarks>A missing row is a failed result with a code, never null. FDW names no table: the application's implementation holds the storage.</remarks>
public interface IReminderLog
{
    /// <summary>Lists the reminders sent about a requirement, oldest first.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="requirementId">The requirement's public id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The reminders, or a failure coded <c>DocumentRecordNotFound</c> when the organization has none of that id as the requirement.</returns>
    Task<IGenericResult<IReadOnlyList<IReminderEntry>>> List(Guid organizationId, Guid requirementId, CancellationToken cancellationToken = default);

    /// <summary>Records a reminder that was sent. A log entry changes no record, so it takes no revision.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="entry">The reminder.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success once recorded, or a failure coded <c>PublicIdInvalid</c> when the id is not a UUIDv7.</returns>
    Task<IGenericResult> Record(Guid organizationId, IReminderEntry entry, CancellationToken cancellationToken = default);
}
