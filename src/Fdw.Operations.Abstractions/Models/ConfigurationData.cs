namespace Fdw.Operations.Clients.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Generic container for configuration data values.
/// </summary>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ConfigurationData : Dictionary<string, object?>
{
    /// <summary>Initializes a new instance of the <see cref="ConfigurationData"/> class.</summary>
    public ConfigurationData() : base(StringComparer.OrdinalIgnoreCase) { }
}
