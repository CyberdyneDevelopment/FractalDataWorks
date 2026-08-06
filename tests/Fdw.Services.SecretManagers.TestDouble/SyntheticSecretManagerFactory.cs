using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.TestDouble;

/// <summary>
/// Builds <see cref="SyntheticSecretManager"/> instances from a declared configuration.
/// </summary>
/// <remarks>
/// Why the header unwrap: <c>DefaultServiceProvider</c> hands the composed
/// <see cref="SecretManagerConfiguration"/> HEADER (with its typed body attached) to the factory, so
/// the factory takes the name from the header and the settings from the body — the same shape every
/// shipped backend factory uses.
/// </remarks>
public sealed class SyntheticSecretManagerFactory : ISyntheticSecretManagerFactory
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntheticSecretManagerFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">Creates the logger the constructed manager writes through.</param>
    public SyntheticSecretManagerFactory(ILoggerFactory loggerFactory)
        => _loggerFactory = loggerFactory ?? throw new System.ArgumentNullException(nameof(loggerFactory));

    /// <inheritdoc />
    public IGenericResult<ISecretManager> Create(SyntheticSecretManagerConfiguration configuration)
        => configuration is null
            ? GenericResult<ISecretManager>.Failure(SecretManagerResultCodes.ByName("InvalidCommandType"))
            : GenericResult<ISecretManager>.Success(
                new SyntheticSecretManager(_loggerFactory.CreateLogger<SyntheticSecretManager>(), configuration));

    /// <inheritdoc />
    public IGenericResult<ISecretManager> Create(IGenericConfiguration configuration)
    {
        // Why the header unwrap: DefaultServiceProvider hands over the composed header with its typed
        // body attached, so the settings come from the body and the logical name from the header.
        if (configuration is SecretManagerConfiguration header
            && header.Configuration is SyntheticSecretManagerConfiguration body)
        {
            body.Name = header.Name;
            return Create(body);
        }

        if (configuration is SyntheticSecretManagerConfiguration direct)
            return Create(direct);

        return GenericResult<ISecretManager>.Failure(SecretManagerResultCodes.ByName("InvalidCommandType"));
    }

    /// <inheritdoc />
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        return result.IsSuccess && result.Value is not null
            ? GenericResult<IGenericService>.Success(result.Value)
            : result.ToNewResult<IGenericService>();
    }

    /// <inheritdoc />
    public IGenericResult<T> Create<T>(IGenericConfiguration configuration)
        where T : IGenericService
    {
        var result = Create(configuration);
        return result.IsSuccess && result.Value is T typed
            ? GenericResult<T>.Success(typed)
            : result.ToNewResult<T>();
    }
}
