using Fdw.Data.DataSets.Results;
using Fdw.Results;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Builds the answer to a category request that is refused.</summary>
public static class CategoryRefusal
{
    /// <summary>A refusal that carries the reason.</summary>
    /// <typeparam name="TValue">The type the request answers.</typeparam>
    /// <param name="reason">Why the category is refused.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult<TValue> Of<TValue>(string reason) =>
        GenericResult<TValue>.Failure(DataSetsResultCodes.ByName("CategoryInvalid"), ResultDetails.Create("reason", reason));
}
