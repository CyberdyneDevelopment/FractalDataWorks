using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>
/// Expands the key a pipeline states (<c>ClaimKeyTemplate</c>, FDW-6939): each <c>{{name}}</c> takes the text of the run's parameter of that name, read without regard to case. Nothing completes
/// a name the run did not state or states with no text, and a placeholder that is not closed or names nothing is refused: a key made of a guess would put two runs on one key or on none.
/// </summary>
public static class PipelineRunClaimKeyTemplate
{
    /// <summary>Expands a template over a run's parameters.</summary>
    /// <param name="template">The pipeline's key template.</param>
    /// <param name="parameters">The run's parameters, by name.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The key text, or the coded failure.</returns>
    public static IGenericResult<string> Expand(string template, IReadOnlyDictionary<string, object?> parameters, ILogger logger)
    {
        var expanded = new StringBuilder();
        var at = 0;
        while (at < template.Length)
        {
            var open = template.IndexOf("{{", at, StringComparison.Ordinal);
            if (open < 0)
            {
                expanded.Append(template, at, template.Length - at);
                break;
            }

            expanded.Append(template, at, open - at);
            var close = template.IndexOf("}}", open + 2, StringComparison.Ordinal);
            if (close < 0 || string.IsNullOrWhiteSpace(template.Substring(open + 2, close - open - 2)))
                return GenericResult<string>.Failure(PipelineRunClaimLog.TemplateMalformed(logger, template));

            var name = template.Substring(open + 2, close - open - 2).Trim();
            var text = parameters.TryGetValue(name, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
            if (string.IsNullOrWhiteSpace(text))
                return GenericResult<string>.Failure(PipelineRunClaimLog.TemplateParameterHasNoValue(logger, name));

            expanded.Append(text);
            at = close + 2;
        }

        return GenericResult<string>.Success(expanded.ToString());
    }
}
