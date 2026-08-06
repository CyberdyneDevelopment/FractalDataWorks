namespace Fdw.Data.RowSources.Http.Abstractions;

/// <summary>
/// Options for GraphQL cursor-based pagination.
/// </summary>
// Why: pure options POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class GraphQlStreamingOptions : HttpRowEnumeratorOptions
{
    /// <summary>
    /// Gets or sets the GraphQL query template.
    /// Use {first} and {after} placeholders for pagination.
    /// </summary>
    public string? QueryTemplate { get; set; }

    /// <summary>
    /// Gets or sets the type name being queried.
    /// </summary>
    public string? TypeName { get; set; }

    /// <summary>
    /// Gets or sets the fields to select.
    /// </summary>
    public string? FieldSelection { get; set; }

    /// <summary>
    /// Gets or sets the path to edges array in response.
    /// Default is "$.data.{typeName}.edges"
    /// </summary>
    public string? EdgesPath { get; set; }

    /// <summary>
    /// Gets or sets the path to pageInfo in response.
    /// Default is "$.data.{typeName}.pageInfo"
    /// </summary>
    public string? PageInfoPath { get; set; }
}