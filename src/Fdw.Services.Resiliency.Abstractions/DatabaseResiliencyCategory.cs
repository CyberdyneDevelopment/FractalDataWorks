using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Resiliency.Abstractions;

/// <summary>
/// Database operations including queries, commands, and transactions.
/// Typically uses moderate retry counts with exponential backoff.
/// </summary>
[TypeOption(typeof(ResiliencyCategories), "Database")]
[ExcludeFromCodeCoverage]
public sealed class DatabaseResiliencyCategory : ResiliencyCategoryBase
{
    /// <summary>Initializes a new instance of <see cref="DatabaseResiliencyCategory"/>.</summary>
    public DatabaseResiliencyCategory() : base(1, "Database") { }
}
