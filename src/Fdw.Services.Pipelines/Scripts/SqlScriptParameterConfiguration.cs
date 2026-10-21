using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Pipelines.Scripts;

/// <summary>A parameter a stored SQL script declares (FDW-1442): the named value its text takes.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Script")]
public sealed partial class SqlScriptParameterConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the parameter's identity.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the parameter's place among the others.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the name as the text writes it after the at sign, without the sign.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type, a <c>DataTypes</c> option name.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether a run must give the parameter a value.</summary>
    public bool IsRequired { get; set; }
}
