namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A parameter a stored script declares: the named value its text takes.</summary>
public class ScriptParameterPayload
{
    /// <summary>Gets or sets the name as the text writes it after the at sign, without the sign.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type, a <c>DataTypes</c> option name.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets whether a run must give the parameter a value.</summary>
    public bool Required { get; set; }
}
