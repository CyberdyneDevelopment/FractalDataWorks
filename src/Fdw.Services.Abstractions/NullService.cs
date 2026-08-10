using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.ServiceTypes.Logging;
using Fdw.ServiceTypes.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Abstractions;

/// <summary>
/// The service of a service type that has no service to build.
/// </summary>
/// <remarks>
/// <c>IServiceType</c> requires a service, a factory and a configuration, because most domains have
/// all three. Some do not: a domain whose whole job is to register endpoints or middleware during
/// the three phases builds nothing a caller resolves by name.
///
/// Those domains had two bad options — invent a per-domain service and factory interface no
/// implementation ever satisfies, or abandon the service-type model and lose the phase machinery.
/// This is the third: say plainly there is no service, once, in a type the reader and the compiler
/// can both see.
///
/// Execute logs and returns a failure. It logs because a call arriving here means something
/// resolved this expecting a service, and that is worth knowing — silence would leave a wiring
/// mistake to surface somewhere unrelated to its cause. It fails rather than returning an empty
/// success because there is no result to give, and a success carrying nothing is indistinguishable
/// from a service that ran and produced nothing.
///
/// It does not throw. An exception decides for the application that the process ends, and a
/// framework does not get to make that call — the host may want to abort or carry on without the
/// domain, and it can only choose if the failure arrives as a value.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class NullService : IGenericService
{
    private readonly ILogger<NullService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NullService"/> class.
    /// </summary>
    /// <param name="logger">The logger; a null logger is used when DI supplies none.</param>
    public NullService(ILogger<NullService>? logger = null)
    {
        _logger = logger ?? NullLogger<NullService>.Instance;
    }

    /// <summary>Gets the singleton instance.</summary>
    public static NullService Instance { get; } = new();

    /// <inheritdoc />
    /// <remarks>
    /// <c>_Empty</c>, the same identity a TypeCollection's generated <c>NotFound</c> sentinel
    /// carries. Both answer the same question — "there is nothing here" — and reading the same way
    /// is the point: an <c>_Empty</c> in a log or a diagnostic means a sentinel was reached, not
    /// that a real thing was misnamed.
    /// </remarks>
    public string Id => SentinelName;

    /// <inheritdoc />
    public string ServiceType => SentinelName;

    /// <summary>
    /// The identity every sentinel in the framework carries.
    /// </summary>
    private const string SentinelName = "_Empty";

    /// <summary>
    /// Gets a value indicating whether this service is available. Always false.
    /// </summary>
    /// <remarks>
    /// False because it is true: there is no service here to be available. A caller checking this
    /// before dispatching gets the right answer without having to call and read a failure.
    /// </remarks>
    public bool IsAvailable => false;

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult<T>.Failure(
            ServiceTypeResultCodes.ByName("NoServiceToExecute"),
            Details(command)));

    /// <inheritdoc />
    public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult((IGenericResult)GenericResult.Failure(
            ServiceTypeResultCodes.ByName("NoServiceToExecute"),
            Details(command)));

    private ResultDetails Details(IGenericCommand? command)
    {
        var commandType = command?.GetType().Name ?? "unknown";
        ServiceTypeLog.NoServiceToExecute(_logger, commandType, nameof(NullService));
        return ResultDetails.Create("CommandType", commandType)
            .With("ServiceTypeName", nameof(NullService));
    }
}
