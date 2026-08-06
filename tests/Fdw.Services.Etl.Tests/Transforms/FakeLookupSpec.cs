using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Minimal <see cref="ILookupSpec"/> test double for <c>LookupTransformType.MapSpecToConfiguration</c> tests.
/// </summary>
internal sealed class FakeLookupSpec : ILookupSpec
{
    /// <inheritdoc />
    public string LookupConnectionName { get; set; } = string.Empty;

    /// <inheritdoc />
    public string LookupDataSet { get; set; } = string.Empty;

    /// <inheritdoc />
    public string LookupKeyField { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SourceKeyField { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? OutputFieldPrefix { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<string> LookupColumns { get; set; } = [];

    /// <inheritdoc />
    public string JoinType { get; set; } = string.Empty;
}
