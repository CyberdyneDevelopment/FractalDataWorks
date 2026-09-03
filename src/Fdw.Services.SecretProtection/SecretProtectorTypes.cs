using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Services.SecretProtection.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.SecretProtection;

/// <summary>
/// Registers <c>ISecretProtector</c> for the whole deployment.
/// </summary>
/// <remarks>
/// A single hand-written three-phase registrant, like <c>TokenManagerTypes</c>, not a
/// <c>[ServiceTypeCollection]</c> dispatch: a deployment runs exactly one protector, never a
/// per-request choice. Unlike <c>TokenManagerTypes</c>, the concrete implementation cannot live in
/// this package -- <c>Fdw.Services.SecretProtection.DataProtection</c> is an optional package this one
/// must not reference, so the implementation attaches itself via <see cref="SetImplementation"/> from
/// its own <c>[ModuleInitializer]</c>, the same "package reference IS registration intent" convention
/// <c>[TypeOption]</c> uses.
/// </remarks>
[ExcludeFromCodeCoverage]
public static class SecretProtectorTypes
{
    private static Action<IServiceCollection, ILoggerFactory?>? _registerImplementation;

    /// <summary>
    /// Called by exactly one implementation package's module initializer to install the registration
    /// this deployment will run. A second caller replacing the first means two protector packages are
    /// referenced -- this does not try to merge or pick; last write wins, same as any other
    /// module-initializer race would.
    /// </summary>
    /// <param name="register">Registers the concrete <c>ISecretProtector</c> and whatever it needs.</param>
    public static void SetImplementation(Action<IServiceCollection, ILoggerFactory?> register)
        => _registerImplementation = register ?? throw new ArgumentNullException(nameof(register));

    /// <summary>No-op -- nothing to bind before Build. Declared only so the three-phase shape is complete.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">Unused.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    public static IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
        => GenericResult<IHostApplicationBuilder>.Success(builder);

    /// <summary>Runs whichever implementation package's module initializer installed itself.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">Optional logger factory for registration logging.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    public static IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        if (defer)
            return GenericResult<IHostApplicationBuilder>.Success(builder);

        var logger = loggerFactory?.CreateLogger(nameof(SecretProtectorTypes)) ?? (ILogger)NullLogger.Instance;

        if (_registerImplementation is null)
            return GenericResult<IHostApplicationBuilder>.Failure(SecretProtectionLog.NoImplementationRegistered(logger));

        try
        {
            _registerImplementation(builder.Services, loggerFactory);
        }
        catch (Exception ex)
        {
            return GenericResult<IHostApplicationBuilder>.Failure(
                SecretProtectionLog.ImplementationRegistrationFailed(logger, ex));
        }

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>No-op -- nothing to eagerly resolve. Declared only so the three-phase shape is complete.</summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">Unused.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    public static IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
        => GenericResult<IHost>.Success(host);
}
