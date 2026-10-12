using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Flow;

/// <summary>
/// Turns proved methods into an assurance level by counting distinct factor kinds.
/// </summary>
/// <remarks>
/// <para>
/// NIST SP 800-63 defines assurance by how many independent factors were used, not how many times
/// someone authenticated. Two passwords are one factor twice, so the count here is of distinct
/// <em>kinds</em> - something known, something held, something inherent.
/// </para>
/// <para>
/// The levels are the deployment's, not this class's: it is handed the declared
/// <see cref="AssuranceLevels"/> and reports the highest one the proved kinds reach. The runner owns the
/// policy, so no step can raise the level it reports (FDW-954).
/// </para>
/// </remarks>
public sealed class StandardAcrPolicy : IAcrPolicy
{
    // RFC 8176 method values, grouped by the kind of factor each represents. A method not listed
    // counts as its own kind rather than being ignored: an unrecognised proof is still a proof, and
    // silently discarding it would understate assurance rather than overstate it.
    private static readonly Dictionary<string, string> FactorKinds = new(StringComparer.Ordinal)
    {
        ["pwd"] = "knowledge",
        ["pin"] = "knowledge",
        ["otp"] = "possession",
        ["sms"] = "possession",
        ["hwk"] = "possession",
        ["swk"] = "possession",
        ["mfa"] = "possession",
        ["fpt"] = "inherence",
        ["face"] = "inherence",
        ["iris"] = "inherence",
        ["vbm"] = "inherence",
        ["retina"] = "inherence",
    };

    private readonly IReadOnlyCollection<IAssuranceLevel> _levels;
    private readonly ILogger<StandardAcrPolicy> _logger;

    /// <summary>Initializes a new instance of the <see cref="StandardAcrPolicy"/> class.</summary>
    /// <param name="levels">The levels the deployment declares; the registration passes <see cref="AssuranceLevels"/>.</param>
    /// <param name="logger">The logger.</param>
    public StandardAcrPolicy(IReadOnlyCollection<IAssuranceLevel> levels, ILogger<StandardAcrPolicy>? logger)
    {
        _levels = levels;
        _logger = logger ?? NullLogger<StandardAcrPolicy>.Instance;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The highest declared level whose factor kinds the proof reaches. No proof, or a proof short of
    /// every declared level, reaches none; so does a deployment that declares no levels at all.
    /// </remarks>
    public string? Evaluate(IReadOnlyList<string> achievedMethods)
    {
        var kinds = achievedMethods
            .Select(m => FactorKinds.TryGetValue(m, out var kind) ? kind : m)
            .Distinct(StringComparer.Ordinal)
            .Count();

        // Nothing proved reaches nothing, whatever a level claims to need.
        if (kinds == 0)
            return null;

        return _levels
            .Where(level => level.FactorKinds <= kinds)
            .OrderByDescending(level => level.FactorKinds)
            .Select(level => level.AcrValue)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    /// <remarks>
    /// A flow demanding nothing is satisfied by anything, including nothing - the terminal check
    /// separately refuses a flow that proved no one, so this cannot be the only gate. A level that no
    /// declared level answers to is a misconfigured flow, and is refused as one rather than read as a
    /// proof that fell short.
    /// </remarks>
    public IGenericResult<bool> Meets(string? achieved, string? required)
    {
        if (required is null)
            return GenericResult<bool>.Success(true);

        if (Level(required) is not { } demanded)
            return GenericResult<bool>.Failure(AcrPolicyLog.LevelNotDeclared(_logger, required));

        if (achieved is null)
            return GenericResult<bool>.Success(false);

        return Level(achieved) is { } reached
            ? GenericResult<bool>.Success(reached.FactorKinds >= demanded.FactorKinds)
            : GenericResult<bool>.Failure(AcrPolicyLog.LevelNotDeclared(_logger, achieved));
    }

    private IAssuranceLevel? Level(string acrValue)
        => _levels.FirstOrDefault(level => string.Equals(level.AcrValue, acrValue, StringComparison.Ordinal));
}
