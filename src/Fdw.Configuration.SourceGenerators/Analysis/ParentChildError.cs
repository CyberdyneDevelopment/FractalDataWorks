namespace Fdw.Configuration.SourceGenerators.Analysis;

/// <summary>
/// Represents a parent/child validation error.
/// </summary>
public class ParentChildError
{
    /// <summary>Gets or sets the configuration name.</summary>
    public string ConfigurationName { get; set; } = "";
    /// <summary>Gets or sets the error type.</summary>
    public ParentChildErrorType ErrorType { get; set; }
    /// <summary>Gets or sets the error message.</summary>
    public string Message { get; set; } = "";
}