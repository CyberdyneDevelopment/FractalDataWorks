using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Pipelines.Scripts;

/// <summary>
/// A stored SQL script (FDW-1442): the text a data engineer wrote, the data store it runs on, whether it selects rows or
/// changes data, and the named parameters its text takes.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Script", ServiceType = "Sql")]
public sealed partial class SqlScriptConfiguration : ISqlScriptImplementationConfiguration
{
    /// <summary>Gets or sets the script's identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the script's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain, which is Script.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the implementation, which is Sql.</summary>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc />
    public string DataStoreName { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Kind { get; set; } = string.Empty;

    /// <inheritdoc />
    public string ScriptText { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <summary>Gets or sets the parameters the text takes.</summary>
    public IList<SqlScriptParameterConfiguration> Parameters { get; set; } = [];
}
