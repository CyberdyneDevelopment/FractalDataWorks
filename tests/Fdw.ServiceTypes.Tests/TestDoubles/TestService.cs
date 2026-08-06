using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.ServiceTypes.Tests.TestDoubles;

// Test service interface
public interface ITestService : IGenericService
{
    string GetData();
}

// Test configuration
public sealed class TestConfiguration : IGenericConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "TestConfig";
    public string SectionName => "TestSection";
    public string ServiceType => "Test";
    public string? ServiceOptionType => "Default";
    public string? ConnectionString { get; init; }
}

// Test factory with configuration
public class TestServiceFactory : IServiceFactory<ITestService, TestConfiguration>
{
    public IGenericResult<ITestService> Create(TestConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    public IGenericResult<ITestService> Create(IGenericConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    public IGenericResult<ITestService> Create(IServiceConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    public IGenericResult<T> Create<T>(IGenericConfiguration configuration) where T : IGenericService
    {
        throw new NotImplementedException();
    }
}

// Test factory without configuration
public class SimpleTestServiceFactory : IServiceFactory<ITestService, IServiceConfiguration>
{
    public IGenericResult<ITestService> Create(IGenericConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    public IGenericResult<ITestService> Create(IServiceConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    public IGenericResult<T> Create<T>(IGenericConfiguration configuration) where T : IGenericService
    {
        throw new NotImplementedException();
    }
}
