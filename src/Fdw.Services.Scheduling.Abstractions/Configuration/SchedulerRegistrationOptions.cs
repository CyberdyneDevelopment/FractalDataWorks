using System;
using Microsoft.Extensions.DependencyInjection;
using Fdw.ServiceTypes;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Concrete implementation of registration options for scheduler services.
/// </summary>
// Why: pure options POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class SchedulerRegistrationOptions : RegistrationOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulerRegistrationOptions"/> class.
    /// </summary>
    public SchedulerRegistrationOptions() : base(ServiceLifetime.Singleton) { }
}
