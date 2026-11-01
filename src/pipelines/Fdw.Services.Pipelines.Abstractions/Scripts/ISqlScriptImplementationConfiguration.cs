using Fdw.Configuration;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>A stored SQL script (FDW-1442): the text a data engineer wrote and where it runs.</summary>
public interface ISqlScriptImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the name of the data store the script runs on.</summary>
    string DataStoreName { get; set; }

    /// <summary>Gets or sets the kind, a <c>ScriptKinds</c> option name: Query or NonQuery.</summary>
    string Kind { get; set; }

    /// <summary>Gets or sets the text, with its values as named parameters such as <c>@Year</c>, never spliced.</summary>
    string ScriptText { get; set; }

    /// <summary>Gets or sets what the script is for.</summary>
    string? Description { get; set; }
}
