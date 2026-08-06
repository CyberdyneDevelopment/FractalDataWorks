using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Configuration.Components.Configuration;
using Fdw.Operations.Clients.Models;

namespace Fdw.Configuration.UI.Components;

/// <summary>
/// Extended configuration context that wraps the FDW ConfigurationContext
/// and adds type-detail (property schema) loading that the base context
/// does not expose.
/// </summary>
public sealed record ConfigurationPageContext(
    ConfigurationContext Inner,
    IReadOnlyList<ConfigurationPropertyInfo> TypeProperties,
    Func<string, string, Task> OnLoadTypeDetail
);
