using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes.Results;

namespace Fdw.Services.Abstractions;

/// <summary>
/// The factory of a service type that has no service to build.
/// </summary>
/// <remarks>
/// Every method fails, and that is the point. A service type closing IServiceType with
/// <see cref="NullService"/> has declared it builds nothing, so a call here means something asked
/// it to build anyway — a wiring mistake, not a condition to absorb. Handing back
/// <see cref="NullService.Instance"/> would let that mistake run and surface somewhere unrelated to
/// its cause.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class NullServiceFactory : IServiceFactory<NullService, IServiceConfiguration>
{
    /// <summary>Gets the singleton instance.</summary>
    public static NullServiceFactory Instance { get; } = new();

    /// <inheritdoc />
    public IGenericResult<NullService> Create(IServiceConfiguration configuration) => Failure<NullService>();

    /// <inheritdoc />
    IGenericResult<NullService> IServiceFactory<NullService>.Create(IGenericConfiguration configuration)
        => Failure<NullService>();

    /// <inheritdoc />
    public IGenericResult<T> Create<T>(IGenericConfiguration configuration)
        where T : IGenericService
        => Failure<T>();

    /// <inheritdoc />
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
        => Failure<IGenericService>();

    private static IGenericResult<T> Failure<T>()
        => GenericResult<T>.Failure(
            ServiceTypeResultCodes.ByName("NoServiceToExecute"),
            ResultDetails.Create("CommandType", "Create")
                .With("ServiceTypeName", nameof(NullServiceFactory)));
}
