using System;
using System.Text;
using System.Text.RegularExpressions;
using Fdw.Roslyn.Commands.Search.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable MA0009 // Regex DoS — pattern is internal-only, derived from caller's glob, with explicit timeout

namespace Fdw.Roslyn.Commands.Search.Translators;

/// <summary>
/// Glob-style namespace matcher supporting <c>*</c> wildcards.
/// </summary>
internal sealed class NamespaceGlobMatcher
{
    private static readonly NamespaceGlobMatcher AcceptAll = new(null, NullLogger.Instance);

    private readonly Regex? _regex;
    private readonly ILogger _logger;

    private NamespaceGlobMatcher(Regex? regex, ILogger logger)
    {
        _regex = regex;
        _logger = logger;
    }

    /// <summary>
    /// Creates a matcher from a glob pattern. <c>null</c>/empty matches everything.
    /// </summary>
    /// <param name="pattern">The glob pattern (or null/empty for accept-all).</param>
    /// <param name="logger">Optional logger; defaults to <see cref="NullLogger.Instance"/>.</param>
    public static NamespaceGlobMatcher Create(string? pattern, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        if (string.IsNullOrEmpty(pattern))
        {
            NamespaceGlobMatcherLog.CreateAcceptAll(logger);
            return AcceptAll;
        }

        var sb = new StringBuilder();
        sb.Append('^');
        foreach (var c in pattern)
        {
            if (c == '*')
                sb.Append(".*");
            else
                sb.Append(Regex.Escape(c.ToString()));
        }
        sb.Append('$');

        var regexPattern = sb.ToString();
        NamespaceGlobMatcherLog.CompiledRegex(logger, pattern, regexPattern);
        return new NamespaceGlobMatcher(new Regex(regexPattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)), logger);
    }
#pragma warning restore MA0009

    /// <summary>
    /// Returns true if the supplied namespace matches the configured glob (or no glob was supplied).
    /// </summary>
    public bool IsMatch(string ns)
    {
        if (_regex is null)
        {
            NamespaceGlobMatcherLog.IsMatchResult(_logger, ns ?? string.Empty, true);
            return true;
        }
        var matched = _regex.IsMatch(ns ?? string.Empty);
        NamespaceGlobMatcherLog.IsMatchResult(_logger, ns ?? string.Empty, matched);
        return matched;
    }
}
