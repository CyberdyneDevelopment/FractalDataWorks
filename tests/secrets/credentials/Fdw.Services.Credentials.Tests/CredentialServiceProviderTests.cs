using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Credentials.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Credentials.Tests;

public sealed class CredentialServiceProviderTests
{
    private const string Name = "UserCredentials";
    private const string Implementation = "Sql";

    private readonly Mock<ICredentialServiceConfigurationProvider> _configurations = new();
    private readonly Mock<IImplementationServiceProvider<ICredentialService, ICredentialServiceImplementationConfiguration>> _implementation = new();
    private readonly Mock<ICredentialServiceImplementationConfiguration> _configuration = new();
    private readonly CredentialServiceProvider _provider = new(NullLogger<CredentialServiceProvider>.Instance);

    public CredentialServiceProviderTests()
    {
        _configuration.SetupGet(c => c.Name).Returns(Name);
        _configuration.SetupGet(c => c.Implementation).Returns(Implementation);
        _implementation.Setup(i => i.Create(It.IsAny<ICredentialServiceImplementationConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<ICredentialService>.Success(new Mock<ICredentialService>().Object));

        _provider.Register(_configurations.Object);
        _provider.Register(Implementation, () => _implementation.Object);
    }

    [Fact]
    public async Task AFailedReadIsNotKeptAndTheNextCallReadsAgain()
    {
        _configurations.SetupSequence(c => c.Get(Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialServiceImplementationConfiguration>.Failure(new GenericMessage("read failed")))
            .ReturnsAsync(GenericResult<ICredentialServiceImplementationConfiguration>.Success(_configuration.Object));

        var first = await _provider.Get(Name, TestContext.Current.CancellationToken);
        var second = await _provider.Get(Name, TestContext.Current.CancellationToken);

        first.IsFailure.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue(second.CurrentMessage);
        _configurations.Verify(c => c.Get(Name, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task TheCallersTokenReachesTheConfigurationRead()
    {
        using var source = new CancellationTokenSource();
        _configurations.Setup(c => c.Get(Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialServiceImplementationConfiguration>.Success(_configuration.Object));

        await _provider.Get(Name, source.Token);

        _configurations.Verify(c => c.Get(Name, source.Token), Times.Once);
    }

    [Fact]
    public async Task EachLookupByNameReadsThroughTheBase()
    {
        _configurations.Setup(c => c.Get(Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialServiceImplementationConfiguration>.Success(_configuration.Object));

        await _provider.Get(Name, TestContext.Current.CancellationToken);
        await _provider.Get(Name, TestContext.Current.CancellationToken);

        _configurations.Verify(c => c.Get(Name, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _implementation.Verify(i => i.Create(_configuration.Object, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ARequestByIdTakesTheSamePath()
    {
        var id = Guid.NewGuid();
        _configurations.Setup(c => c.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialServiceImplementationConfiguration>.Success(_configuration.Object));

        var result = await _provider.Get(new CredentialServiceRequest(id, null), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _implementation.Verify(i => i.Create(_configuration.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnEmptyRequestFails()
    {
        var result = await _provider.Get(new CredentialServiceRequest(null, null), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        _configurations.Verify(c => c.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
