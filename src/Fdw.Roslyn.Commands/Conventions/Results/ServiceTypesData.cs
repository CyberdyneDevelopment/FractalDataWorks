using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Data returned by service types search.
/// </summary>
public sealed class ServiceTypesData
{
    /// <summary>
    /// Gets or sets the total count of service types.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Gets or sets the list of service types.
    /// </summary>
    public required IReadOnlyList<ServiceTypeInfo> ServiceTypes { get; init; }
}