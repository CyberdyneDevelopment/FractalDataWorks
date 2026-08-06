namespace Fdw.Configuration.SourceGenerators.Analysis;

/// <summary>
/// Error types for parent/child relationship validation.
/// </summary>
#pragma warning disable FDW017
public enum ParentChildErrorType
#pragma warning restore FDW017
{
    /// <summary>Parent configuration not found.</summary>
    ParentNotFound,
    /// <summary>Foreign key property is missing.</summary>
    MissingForeignKey,
    /// <summary>Circular parent reference detected.</summary>
    CircularReference
}