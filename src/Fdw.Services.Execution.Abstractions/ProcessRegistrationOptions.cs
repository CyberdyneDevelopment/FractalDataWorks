using System;
using Microsoft.Extensions.DependencyInjection;
using Fdw.ServiceTypes;

namespace Fdw.Services.Execution.Abstractions;

/// <summary>
/// Concrete implementation of registration options for process/execution services.
/// </summary>
// Why: pure options POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ProcessRegistrationOptions : RegistrationOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessRegistrationOptions"/> class.
    /// </summary>
    public ProcessRegistrationOptions() : base(ServiceLifetime.Scoped) { }
}
