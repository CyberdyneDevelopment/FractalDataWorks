using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Results;

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
    public static IGenericResult<IReadOnlyList<string?>> Generate(
        SetDataSetFieldStandInRequest request, int count, IReadOnlyList<string>? drawFromDataSetCandidates = null)
    {
        var handler = StandInHandlers.For(request.Name, request.Field, request.Strategy);
        if (handler.IsFailure)
            return handler.ToNewResult<IReadOnlyList<string?>>();

        var random = CreateRandom(request.Seed);
        var values = new List<string?>(count);

        for (var i = 0; i < count; i++)
        {
            if (request.NullPercent is { } nullPercent && (double)nullPercent / 100 > random.NextDouble())
            {
                values.Add(null);
                continue;
            }

            values.Add(handler.Value!.Generate(request, random, i, drawFromDataSetCandidates));
        }

        return GenericResult<IReadOnlyList<string?>>.Success(values);
    }

    private static Random CreateRandom(string? seed) =>
        seed is null ? new Random() : new Random(seed.GetHashCode(StringComparison.Ordinal));
}
