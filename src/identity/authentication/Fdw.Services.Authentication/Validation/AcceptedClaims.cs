using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Claims.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Reads which platform claims a JwtBearer entry accepts from its issuer's tokens (FDW-2933).
/// </summary>
/// <remarks>
/// The column is one value: the claim names separated by commas, or the word <see cref="None"/> for an entry that accepts no platform claim. An
/// unstated column is not the same as none, so it is refused rather than read as either: the deployer says what this issuer is trusted for. A name
/// that is not a platform claim is refused (a misspelt name would otherwise accept nothing without saying so), and so is a claim the definition marks
/// session-bound, which is the platform's statement about a session it created and which no other issuer may make.
/// </remarks>
public static class AcceptedClaims
{
    /// <summary>The one word that states an entry accepts no platform claim from its issuer.</summary>
    public const string None = "none";

    /// <summary>Reads the column.</summary>
    /// <param name="declared">The entry's column.</param>
    /// <param name="serviceName">The entry, named in failures.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The accepted claim names, none for an entry that states <see cref="None"/>; a failure naming the entry and the claim otherwise.</returns>
    public static IGenericResult<IReadOnlyList<string>> Read(string? declared, string serviceName, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(declared))
        {
            return GenericResult<IReadOnlyList<string>>.Failure(AuthenticationValidationLog.JwtBearerMissingAcceptedClaims(logger, serviceName));
        }

        var names = declared.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (names.Length == 1 && string.Equals(names[0], None, StringComparison.OrdinalIgnoreCase))
        {
            return GenericResult<IReadOnlyList<string>>.Success([]);
        }

        foreach (var name in names)
        {
            var refusal = Refusal(name, serviceName, logger);
            if (refusal.IsFailure)
            {
                return refusal.ToNewResult<IReadOnlyList<string>>();
            }
        }

        return GenericResult<IReadOnlyList<string>>.Success(names);
    }

    private static IGenericResult Refusal(string name, string serviceName, ILogger logger)
    {
        var definition = ClaimDefinitions.ByName(name);
        if (ReferenceEquals(definition, ClaimDefinitions.NotFound))
        {
            return GenericResult.Failure(AuthenticationValidationLog.JwtBearerAcceptedClaimUnknown(
                logger, serviceName, name, string.Join(", ", ClaimDefinitions.All().Select(known => known.Name))));
        }

        return definition.IsSessionBound
            ? GenericResult.Failure(AuthenticationValidationLog.JwtBearerAcceptedClaimSessionBound(logger, serviceName, name))
            : GenericResult.Success();
    }
}
