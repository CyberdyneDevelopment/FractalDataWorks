using System;
using System.IO;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Identity.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity.Authentik.Assertions;

/// <summary>
/// Reads a federated assertion from a file — the shape a projected service-account token takes, where
/// the platform writes the assertion to a path and refreshes it in place.
/// </summary>
/// <remarks>
/// The file is read on every acquisition rather than cached here, because the platform rewrites it as
/// the assertion rotates. Caching the file contents would pin the first assertion and start failing
/// once it expired; the token cache upstream is what keeps this from being a per-request read.
/// </remarks>
[TypeOption(typeof(FederatedAssertionSources), "File")]
public sealed class FileAssertionSource : FederatedAssertionSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="FileAssertionSource"/> class.</summary>
    public FileAssertionSource() : base(2, "File")
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<string> Read(string configurationName, string location, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(location) || !File.Exists(location))
            return GenericResult<string>.Failure(IdentityLog.AssertionNotAvailable(logger, configurationName, Name, location));

        try
        {
            return File.ReadAllText(location).Trim() is { Length: > 0 } assertion
                ? GenericResult<string>.Success(assertion)
                : GenericResult<string>.Failure(IdentityLog.AssertionNotAvailable(logger, configurationName, Name, location));
        }
        catch (IOException ex)
        {
            return GenericResult<string>.Failure(IdentityLog.AssertionUnreadable(logger, ex, configurationName, Name, location));
        }
        catch (UnauthorizedAccessException ex)
        {
            return GenericResult<string>.Failure(IdentityLog.AssertionUnreadable(logger, ex, configurationName, Name, location));
        }
    }
}
