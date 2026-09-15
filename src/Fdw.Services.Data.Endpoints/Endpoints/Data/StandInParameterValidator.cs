using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Confirms a request supplies the parameters its chosen strategy requires.</summary>
internal static class StandInParameterValidator
{
    /// <summary>Confirms every required parameter for <paramref name="strategy"/> is present on <paramref name="request"/>.</summary>
    public static IGenericResult Validate(string dataSetName, string fieldName, string strategy, SetDataSetFieldStandInRequest request)
    {
        switch (strategy)
        {
            case "FixedValue":
                return Require(dataSetName, fieldName, "fixedValue", request.FixedValue is not null);
            case "Sequence":
                return Require(dataSetName, fieldName, "startValue/increment", request.StartValue is not null && request.Increment is not null);
            case "NumericRange":
                return Require(dataSetName, fieldName, "minValue/maxValue", request.MinValue is not null && request.MaxValue is not null);
            case "DateRange":
                return Require(dataSetName, fieldName, "fromDate/toDate", request.FromDate is not null && request.ToDate is not null);
            case "RegexPattern":
                return Require(dataSetName, fieldName, "pattern", !string.IsNullOrEmpty(request.Pattern));
            case "WeightedPick":
                return Require(dataSetName, fieldName, "values", request.Values is { Count: > 0 });
            case "DrawFromDataSet":
                return Require(dataSetName, fieldName, "sourceDataSetId/sourceFieldId", request.SourceDataSetId is not null && request.SourceFieldId is not null);
            default:
                return GenericResult.Success();
        }
    }

    private static IGenericResult Require(string dataSetName, string fieldName, string parameterNames, bool present) =>
        present
            ? GenericResult.Success()
            : GenericResult.Failure(
                DataSetsResultCodes.ByName("StandInParameterMissing"),
                ResultDetails.Create("name", $"{dataSetName}.{fieldName}", "field", parameterNames));
}
