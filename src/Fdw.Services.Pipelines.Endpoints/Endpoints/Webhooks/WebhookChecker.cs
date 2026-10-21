using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Checks a webhook trigger in a request before it is stored (FDW-1884); nothing is assumed in place of what is missing.</summary>
public static class WebhookChecker
{
    private const int LongestName = 200;

    /// <summary>Finds what is wrong with a webhook trigger.</summary>
    /// <param name="payload">The trigger in the request.</param>
    /// <param name="declaredParameters">The names of the parameters the pipeline declares.</param>
    /// <returns>Each problem, in words written for the caller; empty when there is none.</returns>
    public static IReadOnlyList<string> Problems(WebhookPayload payload, IReadOnlyCollection<string> declaredParameters)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(declaredParameters);
        var problems = new List<string>();
        if (!IsAddressName(payload.Name))
        {
            problems.Add($"the name must be 1 to {LongestName} letters, digits, '-', '_' or '.', starting with a letter or digit, because it is part of the address");
        }

        if (string.IsNullOrWhiteSpace(payload.Pipeline))
        {
            problems.Add("the pipeline is not named");
        }

        if (payload.Enabled is null)
        {
            problems.Add("enabled is not stated");
        }

        if (payload.RunRoles.Count == 0)
        {
            problems.Add("no run role is named");
        }

        problems.AddRange(MapProblems(payload, declaredParameters));
        return problems;
    }

    private static IEnumerable<string> MapProblems(WebhookPayload payload, IReadOnlyCollection<string> declaredParameters)
    {
        var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var map in payload.Parameters)
        {
            if (string.IsNullOrWhiteSpace(map.BodyPath) || string.IsNullOrWhiteSpace(map.Parameter))
            {
                yield return "a mapping needs both a body path and a parameter";
            }
            else if (!declaredParameters.Contains(map.Parameter, StringComparer.OrdinalIgnoreCase))
            {
                yield return $"the pipeline '{payload.Pipeline}' does not declare the parameter '{map.Parameter}'";
            }
            else if (!mapped.Add(map.Parameter))
            {
                yield return $"the parameter '{map.Parameter}' is mapped more than once";
            }
        }
    }

    private static bool IsAddressName(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > LongestName || !char.IsAsciiLetterOrDigit(name[0]))
        {
            return false;
        }

        foreach (var character in name)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_' or '.'))
            {
                return false;
            }
        }

        return true;
    }
}
