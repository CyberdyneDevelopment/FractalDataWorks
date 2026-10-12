namespace Fdw.Commands.Abstractions;

/// <summary>
/// Defines the capabilities of a command translator.
/// </summary>
/// <remarks>
/// Translation capabilities help the framework determine which translator
/// can handle specific query operations and optimize query planning.
/// </remarks>
public sealed class TranslationCapabilities
{
    /// <summary>
    /// Gets or sets whether the translator supports projection (SELECT).
    /// </summary>
    public bool SupportsProjection { get; init; }

    /// <summary>
    /// Gets or sets whether the translator supports filtering (WHERE).
    /// </summary>
    public bool SupportsFiltering { get; init; }

    /// <summary>
    /// Gets or sets whether the translator supports ordering (ORDER BY).
    /// </summary>
    public bool SupportsOrdering { get; init; }

    /// <summary>
    /// Gets or sets whether the translator supports paging (SKIP/TAKE).
    /// </summary>
    public bool SupportsPaging { get; init; }

    /// <summary>
    /// Gets or sets whether the translator supports joins.
    /// </summary>
    public bool SupportsJoins { get; init; }

    /// <summary>
    /// Gets or sets whether the translator supports grouping (GROUP BY).
    /// </summary>
    public bool SupportsGrouping { get; init; }

    /// <summary>
    /// Gets or sets whether the translator supports aggregation (COUNT, SUM, etc.).
    /// </summary>
    public bool SupportsAggregation { get; init; }

    /// <summary>
    /// Gets or sets whether the translator supports subqueries.
    /// </summary>
    public bool SupportsSubqueries { get; init; }

    /// <summary>
    /// Gets or sets whether the translator supports transactions.
    /// </summary>
    public bool SupportsTransactions { get; init; }

    /// <summary>
    /// Gets or sets whether the translator supports bulk operations.
    /// </summary>
    public bool SupportsBulkOperations { get; init; }

    /// <summary>
    /// Gets or sets whether the translator supports parameterization.
    /// </summary>
    public bool SupportsParameterization { get; init; }

    /// <summary>
    /// Gets or sets the maximum query complexity this translator can handle.
    /// </summary>
    /// <value>A value from 1-10 indicating complexity support level.</value>
    public int MaxComplexityLevel { get; init; } = 5;

    /// <summary>
    /// Gets a default capability set with all features enabled.
    /// </summary>
    public static TranslationCapabilities Full => new()
    {
        SupportsProjection = true,
        SupportsFiltering = true,
        SupportsOrdering = true,
        SupportsPaging = true,
        SupportsJoins = true,
        SupportsGrouping = true,
        SupportsAggregation = true,
        SupportsSubqueries = true,
        SupportsTransactions = true,
        SupportsBulkOperations = true,
        SupportsParameterization = true,
        MaxComplexityLevel = 10
    };

    /// <summary>
    /// Gets a basic capability set for simple queries.
    /// </summary>
    public static TranslationCapabilities Basic => new()
    {
        SupportsProjection = true,
        SupportsFiltering = true,
        SupportsOrdering = true,
        SupportsPaging = false,
        SupportsJoins = false,
        SupportsGrouping = false,
        SupportsAggregation = false,
        SupportsSubqueries = false,
        SupportsTransactions = false,
        SupportsBulkOperations = false,
        SupportsParameterization = true,
        MaxComplexityLevel = 3
    };
}