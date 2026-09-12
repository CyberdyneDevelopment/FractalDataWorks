using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.TestDouble;

/// <summary>The implementation provider for the mock connection option.</summary>
public interface IMockConnectionProvider
    : IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>
{
}

/// <summary>Builds the mock connection. Resolves nothing, so it completes synchronously.</summary>
public sealed class MockConnectionProvider
    : ImplementationServiceProviderBase<IGenericConnection, IConnectionImplementationConfiguration>,
      IMockConnectionProvider
{
    private readonly IMockConnectionFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="MockConnectionProvider"/> class.</summary>
    /// <param name="factory">Builds the connection.</param>
    public MockConnectionProvider(IMockConnectionFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<IGenericConnection>> Create(
        IConnectionImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_factory.Create(configuration, resolvedSecret: null));
}
