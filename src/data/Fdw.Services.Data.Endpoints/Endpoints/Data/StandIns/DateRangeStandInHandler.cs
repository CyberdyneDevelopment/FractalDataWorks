using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Each value is a random date between the request's two dates.</summary>
[TypeOption(typeof(StandInHandlers), "DateRange", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DateRangeStandInHandler : StandInHandlerBase
{
    /// <summary>Initializes a new instance of the <see cref="DateRangeStandInHandler"/> class.</summary>
    public DateRangeStandInHandler()
        : base("DateRange")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Validate(string dataSetName, string fieldName, SetDataSetFieldStandInRequest request) =>
        Require(dataSetName, fieldName, "fromDate/toDate", request.FromDate is not null && request.ToDate is not null);

    /// <inheritdoc />
    [SuppressMessage("Security", "SCS0005:Weak random number generator", Justification = "Generates sample/preview data, not security-sensitive")]
    public override string? Generate(SetDataSetFieldStandInRequest request, Random random, int ordinal, IReadOnlyList<string>? drawFromDataSetCandidates)
    {
        var span = (request.ToDate!.Value - request.FromDate!.Value).Days;
        var offset = span <= 0 ? 0 : random.Next(0, span + 1);
        return request.FromDate!.Value.AddDays(offset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<Dictionary<string, object?>>> Write(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var row = new DateRangeStandInConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = standIn.Name,
            DataSetFieldStandInId = standIn.Id,
            FromDate = request.FromDate!.Value,
            ToDate = request.ToDate!.Value,
            CreateDate = now,
        };
        var saved = await gateway.Execute<ConfigurationWriteResult>(
            new ConfigurationSaveCommand<DateRangeStandInConfiguration>(row, replacesCurrent: true),
            new DataStoreTarget(dataStoreName, pathName, "DateRangeStandIn"), cancellationToken).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<Dictionary<string, object?>>()
            : GenericResult<Dictionary<string, object?>>.Success(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["fromDate"] = row.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["toDate"] = row.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            });
    }
}
