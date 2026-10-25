using System.Text.Json;
using Fdw.Results;
using Fdw.Services.Settings.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings;

/// <summary>
/// Checks that a body is a home page and nothing more: a JSON object with version 1 and a list of cards (FDW-2325).
/// </summary>
/// <remarks>
/// What a card is, which kinds exist and where a card's data comes from are the console's. Judging them here would close a set a
/// developer is meant to add to, so a card the console draws and this does not know is stored as it was written.
/// </remarks>
internal static class HomePageBody
{
    /// <summary>The version of the page this accepts.</summary>
    internal const int Version = 1;

    /// <summary>Checks a body.</summary>
    /// <param name="body">The page as JSON.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>Success when it is a page; otherwise the refusal, which says what is wrong.</returns>
    internal static IGenericResult Check(string body, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(body))
            return Refuse(logger, "the page is empty");

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return Refuse(logger, "the page is not a JSON object");

            if (!root.TryGetProperty("version", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var number) || number != Version)
                return Refuse(logger, $"the page needs \"version\": {Version}");

            return root.TryGetProperty("cards", out var cards) && cards.ValueKind == JsonValueKind.Array
                ? GenericResult.Success()
                : Refuse(logger, "the page needs a \"cards\" list");
        }
        catch (JsonException ex)
        {
            return Refuse(logger, $"the page is not valid JSON: {ex.Message}");
        }
    }

    private static IGenericResult Refuse(ILogger logger, string reason) => GenericResult.Failure(HomePagesLog.BodyInvalid(logger, reason));
}
