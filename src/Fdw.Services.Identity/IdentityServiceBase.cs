using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Commands;
using Fdw.Services.Identity.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity;

/// <summary>
/// Base class for identity services. Supplies the generic <c>IGenericService.Execute</c> surface by
/// routing <see cref="IdentityTokenCommand"/> to the same acquisition
/// <see cref="IIdentityService.Acquire"/> uses, so there is exactly one implementation of the
/// operation regardless of which surface a caller came in through.
/// </summary>
/// <typeparam name="TConfiguration">The typed configuration body this mechanism reads.</typeparam>
/// <typeparam name="TService">The concrete service type, for the logging category.</typeparam>
public abstract class IdentityServiceBase<TConfiguration, TService>
    : ServiceBase<IdentityTokenCommand, TConfiguration, TService>, IIdentityService
    where TConfiguration : class, IIdentityServiceImplementationConfiguration
    where TService : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityServiceBase{TConfiguration, TService}"/> class.
    /// </summary>
    /// <param name="logger">The logger for the concrete service type.</param>
    /// <param name="configuration">The typed configuration body for this identity.</param>
    protected IdentityServiceBase(ILogger<TService>? logger, TConfiguration configuration)
        : base(logger, configuration)
    {
    }

    /// <inheritdoc/>
    public abstract Task<IGenericResult<IssuedIdentityToken>> Acquire(IdentityTokenRequest request, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public override async Task<IGenericResult> Execute(IdentityTokenCommand command, CancellationToken cancellationToken = default)
        => await Acquire(command.Request, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public override async Task<IGenericResult<T>> Execute<T>(IdentityTokenCommand command, CancellationToken cancellationToken = default)
    {
        var acquired = await Acquire(command.Request, cancellationToken).ConfigureAwait(false);
        if (acquired.IsFailure)
            return acquired.ToNewResult<T>();

        return acquired.Value is T typed
            ? GenericResult<T>.Success(typed)
            : GenericResult<T>.Failure(IdentityLog.ResultTypeMismatch(Logger, typeof(T).Name, nameof(IssuedIdentityToken)));
    }
}
