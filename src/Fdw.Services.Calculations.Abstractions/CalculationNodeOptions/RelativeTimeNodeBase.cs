using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.RelativeTime;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// An operator that works with a moment in time: it states the time zone in <c>name</c> and one option of a TypeCollection in
/// <c>mode</c>. The zone has no default; whether <c>mode</c> may be left out is the operator's own rule.
/// </summary>
/// <remarks>
/// Why the zone is stated on every node: a day, a week, a month and a year start at different moments in different zones, and a
/// server that assumed one would give a wrong answer without saying so.
/// </remarks>
public abstract class RelativeTimeNodeBase : CalculationNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RelativeTimeNodeBase"/> class.
    /// </summary>
    /// <param name="name">The operator's name.</param>
    /// <param name="summary">A one-sentence description.</param>
    /// <param name="operands">How many operands it takes.</param>
    protected RelativeTimeNodeBase(string name, string summary, int operands)
        : base(name, summary, operands, operands)
    {
    }

    /// <inheritdoc />
    public sealed override IReadOnlyList<string> Members { get; } = new[] { "name", "mode" };

    /// <inheritdoc />
    public sealed override IReadOnlyList<string> Modes => ModeNames;

    /// <inheritdoc />
    public sealed override Type ResultType(CalculationNode node, Type expected, IReadOnlyList<Type> operandTypes) =>
        typeof(DateTimeOffset);

    /// <summary>Gets the names of the options <c>mode</c> may take.</summary>
    protected abstract IReadOnlyList<string> ModeNames { get; }

    /// <summary>Gets what <c>mode</c> names, for a refusal.</summary>
    protected abstract string ModeWord { get; }

    /// <summary>Gets a value indicating whether the operator means something without <c>mode</c>.</summary>
    protected virtual bool ModeIsOptional => false;

    /// <inheritdoc />
    public sealed override IGenericResult Check(CalculationNode node, ICalculationScope scope)
    {
        if (node.Name is not { } zoneId || string.IsNullOrWhiteSpace(zoneId))
        {
            return Invalid(node, $"a {node.Op} needs the time zone in 'name', an IANA id such as America/Chicago; none is assumed");
        }

        if (!RelativeTimeZones.TryFind(zoneId, out _, out var reason))
        {
            return Invalid(node, $"time zone '{zoneId}' is not known to this server: {reason}");
        }

        if (node.Mode is null)
        {
            return ModeIsOptional
                ? GenericResult.Success()
                : Invalid(node, $"a {node.Op} needs {ModeWord} in 'mode', one of {string.Join(", ", ModeNames)}; none is assumed");
        }

        return ModeNames.Contains(node.Mode, StringComparer.Ordinal)
            ? GenericResult.Success()
            : Invalid(node, $"'{node.Mode}' is not {ModeWord}; use one of {string.Join(", ", ModeNames)}");
    }

    /// <summary>Gets the time zone a node states. Check refuses a node whose zone is missing or unknown before this is asked.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The zone.</returns>
    protected static TimeZoneInfo ZoneOf(CalculationNode node)
    {
        if (node.Name is { } zoneId && RelativeTimeZones.TryFind(zoneId, out var zone, out _) && zone is not null)
        {
            return zone;
        }

        throw new InvalidOperationException($"A {node.Op} node without a known time zone reached Emit; Check refuses it");
    }
}
