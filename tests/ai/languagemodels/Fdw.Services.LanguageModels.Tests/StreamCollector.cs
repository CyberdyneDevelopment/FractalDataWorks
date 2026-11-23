using Fdw.Services.LanguageModels.Abstractions;

namespace Fdw.Services.LanguageModels.Tests;

internal static class StreamCollector
{
    public static async Task<List<LanguageModelStreamEvent>> Collect(IAsyncEnumerable<LanguageModelStreamEvent> stream)
    {
        var events = new List<LanguageModelStreamEvent>();
        await foreach (var streamEvent in stream)
            events.Add(streamEvent);
        return events;
    }
}
