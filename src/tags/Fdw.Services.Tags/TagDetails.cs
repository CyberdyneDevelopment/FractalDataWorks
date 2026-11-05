using Fdw.Results;

namespace Fdw.Services.Tags;

/// <summary>Builds the details of a tags refusal from key and value pairs, however many there are.</summary>
internal static class TagDetails
{
    /// <summary>Builds result details from alternating keys and values.</summary>
    /// <param name="pairs">Alternating key (a string) and value.</param>
    /// <returns>The details.</returns>
    public static ResultDetails Of(params object?[] pairs)
    {
        var details = ResultDetails.Create();
        for (var index = 0; index + 1 < pairs.Length; index += 2)
        {
            details = details.With(pairs[index] as string ?? string.Empty, pairs[index + 1]);
        }

        return details;
    }
}
