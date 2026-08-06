using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Tests.TestHelpers;

/// <summary>
/// Test service for unit testing service factory functionality.
/// </summary>
public class TestService : IGenericService
{
    public TestService(ILogger<TestService> logger, TestConfiguration configuration)
    {
        Logger = logger;
        Configuration = configuration;
    }

    public ILogger<TestService> Logger { get; }
    public TestConfiguration Configuration { get; }

    public string Id => Configuration.Id.ToString();
    public string ServiceType => nameof(TestService);
    public bool IsAvailable => true;

    public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(GenericResult<T>.Success(default(T)!));
    }

    public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(GenericResult.Success());
    }
}

/// <summary>
/// Test configuration for unit testing.
/// </summary>
public sealed class TestConfiguration : IGenericConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "test-config";
    public string SectionName => "TestSection";
    public string ServiceType => "Test";
    public string? ServiceOptionType { get; set; } = "Default";
    public string? Description { get; init; }
}

/// <summary>
/// Test service factory for unit testing.
/// </summary>
public class TestServiceFactory : ServiceFactory<TestService, TestConfiguration>
{
    public TestServiceFactory(ILogger<TestService>? logger = null)
        : base(logger)
    {
    }
}

/// <summary>
/// Another test service type for testing type mismatches.
/// </summary>
public class AnotherTestService : IGenericService
{
    public AnotherTestService(ILogger<AnotherTestService> logger, TestConfiguration configuration)
    {
        Logger = logger;
        Configuration = configuration;
    }

    public ILogger<AnotherTestService> Logger { get; }
    public TestConfiguration Configuration { get; }

    public string Id => Configuration.Id.ToString();
    public string ServiceType => nameof(AnotherTestService);
    public bool IsAvailable => true;

    public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(GenericResult<T>.Success(default(T)!));
    }

    public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(GenericResult.Success());
    }
}
