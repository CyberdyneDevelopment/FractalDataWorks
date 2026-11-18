using System.Collections.Generic;
using Fdw.Data.Options;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>
/// Checks a data option path and method the way the rule service does when it adds or changes a rule (FDW-6566), so the path check route and the rule routes refuse a path with the same
/// codes: an empty level is DATA-21685, a name the level does not have DATA-31682 or DATA-31683, a path that ends before a kind DATA-21686, one that goes past a kind or names another
/// method than the kind's DATA-31684.
/// </summary>
public static class DataOptionPathCheck
{
    /// <summary>Resolves the path and method to the kind they name, or says why they do not.</summary>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <param name="path">The names from the DataConfigurationRules root down to a kind; at least one.</param>
    /// <param name="method">The method the path ends in; not blank.</param>
    /// <returns>The kind; or the coded refusal that names the level that failed.</returns>
    public static IGenericResult<IRuleKind> Resolve(ILogger logger, IReadOnlyList<string> path, string method)
        => DataConfigurationRuleChecks.Kind(logger, string.Empty, path, method);
}
