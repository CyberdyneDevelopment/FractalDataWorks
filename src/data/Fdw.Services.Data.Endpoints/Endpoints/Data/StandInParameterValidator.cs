using Fdw.Results;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Confirms a request supplies the parameters its chosen strategy requires.</summary>
internal static class StandInParameterValidator
{
    /// <summary>Confirms every required parameter for <paramref name="strategy"/> is present on <paramref name="request"/>.</summary>
    public static IGenericResult Validate(string dataSetName, string fieldName, string strategy, SetDataSetFieldStandInRequest request)
    {
        var handler = StandInHandlers.For(dataSetName, fieldName, strategy);
        return handler.IsFailure ? handler : handler.Value!.Validate(dataSetName, fieldName, request);
    }
}
