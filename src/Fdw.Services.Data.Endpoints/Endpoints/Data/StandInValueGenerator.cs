using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Generates example values for a stand-in strategy, honouring NullPercent and Seed.</summary>
/// <remarks>
/// Pure and synchronous except for DrawFromDataSet, whose candidate pool the caller resolves first
/// (a real data read) and passes in — this class never reaches the gateway itself.
/// </remarks>
internal static class StandInValueGenerator
{
    /// <summary>Generates <paramref name="count"/> example values for the strategy the request names.</summary>
    /// <param name="request">The strategy and its parameters.</param>
    /// <param name="count">How many values to generate.</param>
    /// <param name="drawFromDataSetCandidates">
    /// The source field's known values, for DrawFromDataSet only — resolved by the caller from that
    /// field's own samples, since this class does not read data itself.
    /// </param>
    [SuppressMessage("Security", "SCS0005:Weak random number generator", Justification = "Generates sample/preview data, not security-sensitive")]
    public static IReadOnlyList<string?> Generate(
        SetDataSetFieldStandInRequest request, int count, IReadOnlyList<string>? drawFromDataSetCandidates = null)
    {
        var random = CreateRandom(request.Seed);
        var values = new List<string?>(count);

        for (var i = 0; i < count; i++)
        {
            if (request.NullPercent is { } nullPercent && (double)nullPercent / 100 > random.NextDouble())
            {
                values.Add(null);
                continue;
            }

            values.Add(GenerateOne(request, random, i, drawFromDataSetCandidates));
        }

        return values;
    }

    private static Random CreateRandom(string? seed) =>
        seed is null ? new Random() : new Random(seed.GetHashCode(StringComparison.Ordinal));

    private static string? GenerateOne(
        SetDataSetFieldStandInRequest request, Random random, int ordinal, IReadOnlyList<string>? drawFromDataSetCandidates) =>
        request.Strategy switch
        {
            "FixedValue" => request.FixedValue,
            "Sequence" => (request.StartValue!.Value + (ordinal * request.Increment!.Value)).ToString(CultureInfo.InvariantCulture),
            "NumericRange" => GenerateNumericRange(request, random),
            "DateRange" => GenerateDateRange(request, random),
            "RegexPattern" => RegexPatternGenerator.Generate(request.Pattern!, random),
            "WeightedPick" => GenerateWeightedPick(request.Values!, random),
            "DrawFromDataSet" => GenerateDrawFromDataSet(drawFromDataSetCandidates, random),
            _ => null,
        };

    [SuppressMessage("Security", "SCS0005:Weak random number generator", Justification = "Generates sample/preview data, not security-sensitive")]
    private static string GenerateNumericRange(SetDataSetFieldStandInRequest request, Random random)
    {
        var value = request.MinValue!.Value + ((decimal)random.NextDouble() * (request.MaxValue!.Value - request.MinValue!.Value));
        var rounded = request.Decimals is { } decimals ? Math.Round(value, decimals) : Math.Round(value, 0);
        return rounded.ToString(CultureInfo.InvariantCulture);
    }

    [SuppressMessage("Security", "SCS0005:Weak random number generator", Justification = "Generates sample/preview data, not security-sensitive")]
    private static string GenerateDateRange(SetDataSetFieldStandInRequest request, Random random)
    {
        var span = (request.ToDate!.Value - request.FromDate!.Value).Days;
        var offset = span <= 0 ? 0 : random.Next(0, span + 1);
        return request.FromDate!.Value.AddDays(offset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    [SuppressMessage("Security", "SCS0005:Weak random number generator", Justification = "Generates sample/preview data, not security-sensitive")]
    private static string GenerateWeightedPick(List<WeightedPickValueRequest> options, Random random)
    {
        var totalWeight = options.Sum(o => o.Weight);
        var roll = random.Next(0, Math.Max(totalWeight, 1));
        var cumulative = 0;
        foreach (var option in options.OrderBy(o => o.Ordinal))
        {
            cumulative += option.Weight;
            if (roll < cumulative) return option.Value;
        }

        return options[^1].Value;
    }

    [SuppressMessage("Security", "SCS0005:Weak random number generator", Justification = "Generates sample/preview data, not security-sensitive")]
    private static string? GenerateDrawFromDataSet(IReadOnlyList<string>? candidates, Random random) =>
        candidates is { Count: > 0 } ? candidates[random.Next(candidates.Count)] : null;
}
