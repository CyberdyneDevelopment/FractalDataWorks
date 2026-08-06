using System;
using Microsoft.Extensions.DependencyInjection;
using Fdw.ServiceTypes;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Concrete implementation of registration options for connection services.
/// </summary>
// Why: pure options POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ConnectionRegistrationOptions : RegistrationOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionRegistrationOptions"/> class.
    /// </summary>
    public ConnectionRegistrationOptions() : base(ServiceLifetime.Scoped) { }
}
