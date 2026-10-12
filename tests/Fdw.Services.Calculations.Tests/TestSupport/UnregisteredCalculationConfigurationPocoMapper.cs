using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;

namespace Fdw.Services.Calculations.Tests.TestSupport;

/// <summary>
/// The mapper the generator would write for <see cref="UnregisteredCalculationConfiguration"/>, written by hand:
/// this project runs no mapper generator.
/// </summary>
/// <remarks>
/// The calculation domain stamps the fields its list shows onto a record it reads, through that record's mapper,
/// so a record with no mapper is refused before any kind is looked up. A test that wants a record whose kind no
/// option claims registers this one first. Only the parts a read reaches are written; reading a row into the
/// record is not one of them, and says so rather than answering something.
/// </remarks>
internal sealed class UnregisteredCalculationConfigurationPocoMapper()
    : PocoMapperBase(
        "Fdw.Services.Calculations.Tests.TestSupport.UnregisteredCalculationConfiguration",
        typeof(UnregisteredCalculationConfiguration))
{
    private static readonly string[] Properties =
    [
        "Id", "Name", "Domain", "Implementation", "CalculationSource", "Description",
        "OutputDataSetName", "ResultFieldName", "ResultDataTypeName", "IsEnabled",
    ];

    /// <inheritdoc />
    public override IGenericResult<object> MapFromReader(DbDataReader reader, IStorageContainer container)
        => throw new NotSupportedException("This test record is never read from a row.");

    /// <inheritdoc />
    public override IGenericResult<object> MapFromDictionary(IDictionary<string, object?> data)
        => throw new NotSupportedException("This test record is never read from a row.");

    /// <inheritdoc />
    public override IReadOnlyList<string> GetPropertyNames() => Properties;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> MapToParameters(object instance)
    {
        var record = (UnregisteredCalculationConfiguration)instance;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Id"] = record.Id,
            ["Name"] = record.Name,
            ["Domain"] = record.Domain,
            ["Implementation"] = record.Implementation,
            ["CalculationSource"] = record.CalculationSource,
            ["Description"] = record.Description,
            ["OutputDataSetName"] = record.OutputDataSetName,
            ["ResultFieldName"] = record.ResultFieldName,
            ["ResultDataTypeName"] = record.ResultDataTypeName,
            ["IsEnabled"] = record.IsEnabled,
        };
    }

    /// <inheritdoc />
    public override void SetValue(object instance, string columnName, object? value)
    {
        var record = (UnregisteredCalculationConfiguration)instance;
        switch (columnName)
        {
            case "Id": record.Id = (Guid)value!; break;
            case "Name": record.Name = (string)value!; break;
            case "Domain": record.Domain = (string)value!; break;
            case "Implementation": record.Implementation = (string)value!; break;
            case "CalculationSource": record.CalculationSource = (string)value!; break;
            case "Description": record.Description = (string?)value; break;
            case "OutputDataSetName": record.OutputDataSetName = (string?)value; break;
            case "ResultFieldName": record.ResultFieldName = (string?)value; break;
            case "ResultDataTypeName": record.ResultDataTypeName = (string)value!; break;
            case "IsEnabled": record.IsEnabled = (bool)value!; break;
            default: throw new ArgumentOutOfRangeException(nameof(columnName), columnName, "The test record has no such property.");
        }
    }

    /// <inheritdoc />
    public override IList CreateList() => new List<UnregisteredCalculationConfiguration>();

    /// <inheritdoc />
    public override Array CreateArray(int length) => new UnregisteredCalculationConfiguration[length];

    /// <inheritdoc />
    public override IGenericConfiguration? GetTypedBody(object parent) => null;

    /// <inheritdoc />
    public override void SetTypedBody(object parent, IGenericConfiguration? body)
    {
    }

    /// <inheritdoc />
    public override IReadOnlyList<IChildCascadeDescriptor> CascadeChildren { get; } = Array.Empty<IChildCascadeDescriptor>();
}
