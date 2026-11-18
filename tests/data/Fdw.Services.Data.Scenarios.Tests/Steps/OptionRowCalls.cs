using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Data.Scenarios.Tests.Steps;

/// <summary>The calls to the option row routes that steps and the after-scenario hook share.</summary>
internal static class OptionRowCalls
{
    internal static string Base(string container) => $"{ScenarioState.ContainerBase(container)}/options";

    internal static string AddBody(IEnumerable<(string Column, string Value, string OptionType, string Option, bool? Active)> rows)
        => JsonSerializer.Serialize(new
        {
            rows = rows.Select(row => row.Active is { } active
                ? (object)new { column = row.Column, value = row.Value, optionType = row.OptionType, option = row.Option, active }
                : new { column = row.Column, value = row.Value, optionType = row.OptionType, option = row.Option }),
        });

    internal static async Task<IReadOnlyList<(string Id, string Column, string Value)>> ListAsync(ScenarioState state, Permission person, string container, CancellationToken cancellationToken)
    {
        var answer = await state.SendAsAsync(person, HttpMethod.Get, Base(container), null, cancellationToken).ConfigureAwait(false);
        answer.Status.ShouldBe(200, answer.Body);
        using var json = answer.Json();
        return json.RootElement.GetProperty("items").EnumerateArray()
            .Select(item => (item.GetProperty("id").GetString()!, item.GetProperty("column").GetString()!, item.GetProperty("value").GetString()!))
            .ToArray();
    }

    internal static async Task RemoveAllAsync(ScenarioState state, string container, CancellationToken cancellationToken)
    {
        while (true)
        {
            var held = await ListAsync(state, Permission.Write, container, cancellationToken).ConfigureAwait(false);
            if (held.Count == 0)
                return;
            var query = string.Join('&', held.Select(row => $"id={Uri.EscapeDataString(row.Id)}"));
            var answer = await state.SendAsAsync(Permission.Write, HttpMethod.Delete, $"{Base(container)}?{query}", null, cancellationToken).ConfigureAwait(false);
            answer.Status.ShouldBe(204, answer.Body);
        }
    }

    internal static async Task<(string Container, string Id)> FindAsync(ScenarioState state, string value, CancellationToken cancellationToken)
    {
        foreach (var container in state.WrittenContainers)
        {
            var held = await ListAsync(state, Permission.Write, container, cancellationToken).ConfigureAwait(false);
            var match = held.Where(row => row.Value == value).ToArray();
            if (match.Length == 1)
                return (container, match[0].Id);
        }

        throw new InvalidOperationException($"No option row with the value {value} is held by any container this scenario wrote to.");
    }
}
