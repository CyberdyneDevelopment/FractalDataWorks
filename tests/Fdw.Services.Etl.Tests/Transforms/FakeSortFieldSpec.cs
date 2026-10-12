using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Minimal <see cref="ISortFieldSpec"/> test double.
/// </summary>
internal sealed class FakeSortFieldSpec : ISortFieldSpec
{
    /// <summary>Initializes a new instance of the <see cref="FakeSortFieldSpec"/> class.</summary>
    /// <param name="fieldName">The field to sort by.</param>
    /// <param name="direction">The direction name.</param>
    public FakeSortFieldSpec(string fieldName, string direction)
    {
        FieldName = fieldName;
        Direction = direction;
    }

    /// <inheritdoc />
    public string FieldName { get; }

    /// <inheritdoc />
    public string Direction { get; }
}
