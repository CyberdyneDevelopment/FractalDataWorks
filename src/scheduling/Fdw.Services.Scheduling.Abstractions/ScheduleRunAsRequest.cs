using System;
using System.Collections.Generic;

namespace Fdw.Services.Scheduling.Clients.Abstractions;

/// <summary>
/// What a person states to have a schedule's runs act as them (FDW-6144): what the runs may do while the person is away, and until when. The person is the signed-in caller; no field names one.
/// </summary>
public class ScheduleRunAsRequest
{
    /// <summary>
    /// Gets or sets the permissions the runs may use while the person is away. Each must be one the caller holds, and together they must cover every permission the schedule's run roles carry.
    /// Required unless <see cref="Remove"/> is set.
    /// </summary>
    public IList<string>? Permissions { get; set; }

    /// <summary>Gets or sets when the grant ends. Required unless <see cref="Remove"/> is set; later than now.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Gets or sets a value indicating whether the schedule's runs stop acting as the person: the grant is revoked and later runs act as the schedule's tenant. Update only.</summary>
    public bool Remove { get; set; }
}
