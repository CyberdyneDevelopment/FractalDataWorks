using System;
using Microsoft.Extensions.DependencyInjection;
using Fdw.ServiceTypes;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Concrete implementation of registration options for data services.
/// </summary>
// Why: pure options POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class DataRegistrationOptions : RegistrationOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataRegistrationOptions"/> class.
    /// </summary>
    public DataRegistrationOptions() : base(ServiceLifetime.Scoped) { }
}
