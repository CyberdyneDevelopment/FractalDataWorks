using Fdw.Results;
using Fdw.Services.Etl.Results;

namespace Fdw.Services.Etl.Scripts;

/// <summary>Builds the answer to a script request that is refused.</summary>
public static class ScriptRefusal
{
    /// <summary>A refusal that carries the reason.</summary>
    /// <typeparam name="TValue">The type the request answers.</typeparam>
    /// <param name="reason">Why the script is refused.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult<TValue> Of<TValue>(string reason) =>
        GenericResult<TValue>.Failure(EtlResultCodes.ByName("InvalidScript"), ResultDetails.Create("Reason", reason));
}
