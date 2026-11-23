using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Options.Logging;
using Fdw.Data.Options.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Options;

/// <summary>
/// Walks a stored rule path through the nested layers to its kind (FDW-6340), to any depth. One failure per reason: a level that names
/// nothing, a path that stops before a kind, a path that goes on past a kind, and a method the kind does not have.
/// </summary>
public static class RulePaths
{
    /// <summary>The text that joins the names of a stored path, as in <c>DataConfigurationRules:Comparison:In.To</c>.</summary>
    public const string PathSeparator = ":";

    /// <summary>The character that ends a stored path and starts its method.</summary>
    public const char MethodSeparator = '.';

    /// <summary>Splits the one text a rule stores for its path, such as <c>DataConfigurationRules:Comparison:In.To</c>, into the path's names and the method.</summary>
    /// <param name="stored">The stored text.</param>
    /// <param name="path">The path's names, when the text splits.</param>
    /// <param name="method">The method after the last period, when the text splits.</param>
    /// <returns><see langword="true"/> when the text has a path and a method; <see langword="false"/> when it has no period, or nothing before or after it.</returns>
    public static bool TrySplit(string stored, out IReadOnlyList<string> path, out string method)
    {
        var split = stored.LastIndexOf(MethodSeparator);
        if (split <= 0 || split == stored.Length - 1)
        {
            path = [];
            method = string.Empty;
            return false;
        }

        path = stored.Substring(0, split).Split(PathSeparator[0]);
        method = stored.Substring(split + 1);
        return true;
    }

    /// <summary>Resolves <paramref name="path"/> and <paramref name="method"/> to the kind they name.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="path">The path's names, from the DataConfigurationRules root down to the kind.</param>
    /// <param name="method">The method the path ends in, after the period.</param>
    /// <returns>The kind, or the failure that says which level did not resolve.</returns>
    public static IGenericResult<IRuleKind> Resolve(ILogger logger, IReadOnlyList<string> path, string method)
    {
        var text = string.Join(":", path);
        if (path.Count == 0 || !string.Equals(path[0], nameof(DataConfigurationRules), StringComparison.OrdinalIgnoreCase))
        {
            return LevelNotFound(logger, text, 1, path.Count == 0 ? string.Empty : path[0], nameof(DataConfigurationRules));
        }

        IReadOnlyList<IRuleLayer> layers = [.. DataConfigurationRules.All()];
        for (var index = 1; index < path.Count; index++)
        {
            var name = path[index];
            var layer = layers.FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
            if (layer is null)
            {
                return LevelNotFound(logger, text, index + 1, name, Known(layers));
            }

            if (layer is IRuleKind kind)
            {
                return index < path.Count - 1
                    ? PastKind(logger, text, path[index + 1], kind.Name)
                    : AtKind(logger, text, kind, method);
            }

            if (index == path.Count - 1)
            {
                return BeforeKind(logger, text, layer.Name, Known(layer.Layers));
            }

            layers = layer.Layers;
        }

        return BeforeKind(logger, text, path[^1], Known(layers));
    }

    private static string Known(IReadOnlyList<IRuleLayer> layers) => string.Join(", ", layers.Select(layer => layer.Name));

    private static IGenericResult<IRuleKind> AtKind(ILogger logger, string text, IRuleKind kind, string method)
    {
        if (string.Equals(kind.Method, method, StringComparison.Ordinal))
        {
            return GenericResult<IRuleKind>.Success(kind);
        }

        DataOptionLog.MethodNotFound(logger, text, method, kind.Method);
        return GenericResult<IRuleKind>.Failure(
            DataOptionResultCodes.ByName("DataOptionMethodNotFound"),
            ResultDetails.Create("Path", text, "Method", method).With("KindMethod", kind.Method));
    }

    private static IGenericResult<IRuleKind> LevelNotFound(ILogger logger, string text, int level, string name, string known)
    {
        DataOptionLog.PathLevelNotFound(logger, text, level, name, known);
        return GenericResult<IRuleKind>.Failure(
            DataOptionResultCodes.ByName("DataOptionPathLevelNotFound"),
            ResultDetails.Create("Path", text, "Level", level).With("Name", name).With("Known", known));
    }

    private static IGenericResult<IRuleKind> BeforeKind(ILogger logger, string text, string name, string known)
    {
        DataOptionLog.PathEndsBeforeKind(logger, text, name, known);
        return GenericResult<IRuleKind>.Failure(
            DataOptionResultCodes.ByName("DataOptionPathEndsBeforeKind"),
            ResultDetails.Create("Path", text, "Name", name).With("Known", known));
    }

    private static IGenericResult<IRuleKind> PastKind(ILogger logger, string text, string extra, string kind)
    {
        DataOptionLog.PathContinuesPastKind(logger, text, extra, kind);
        return GenericResult<IRuleKind>.Failure(
            DataOptionResultCodes.ByName("DataOptionPathContinuesPastKind"),
            ResultDetails.Create("Path", text, "Extra", extra).With("Kind", kind));
    }
}
