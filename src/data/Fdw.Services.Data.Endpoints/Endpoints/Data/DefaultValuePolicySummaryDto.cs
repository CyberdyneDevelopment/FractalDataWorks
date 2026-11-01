namespace Fdw.Services.Data.Endpoints;

/// <summary>A default-value policy a connection of a given type can name.</summary>
public class DefaultValuePolicySummaryDto
{
    /// <summary>Gets or sets the policy name -- the value a connection's <c>DefaultValuePolicy</c> takes.</summary>
    public string Name { get; set; } = string.Empty;
}
