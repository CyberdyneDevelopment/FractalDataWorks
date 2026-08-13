using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fdw.Services.Abstractions.Tests;

/// <summary>
/// Tests for ServiceTypeBase null handling and edge cases
/// </summary>
public class ServiceTypeBaseNullHandlingTests
{
    [ExcludeFromCodeCoverage]
    private class SimpleCommand : IGenericCommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string CommandType => "Simple";
        public string Category => "Test";
    }

    [ExcludeFromCodeCoverage]
    private class SimpleConfig : IGenericConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Test";
        public string SectionName => "Test";
        public string ServiceType => "Test";
        public string? ServiceOptionType => "Test";
    }

    [ExcludeFromCodeCoverage]
    private class SimpleService : IGenericService
    {
        public string Id => "test";
        public string ServiceType => "Test";
        public bool IsAvailable => true;

        public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
            => Task.FromResult(GenericResult<T>.Success(default!));

        public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken)
            => Task.FromResult(GenericResult.Success());
    }

    [ExcludeFromCodeCoverage]
    private class SimpleFactory : IServiceFactory<SimpleService, SimpleConfig>
    {
        public IGenericResult<SimpleService> Create(SimpleConfig configuration)
            => GenericResult<SimpleService>.Success(new SimpleService());

        public IGenericResult<SimpleService> Create(IGenericConfiguration configuration)
            => GenericResult<SimpleService>.Success(new SimpleService());

        IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
            => GenericResult<IGenericService>.Success(new SimpleService());

        IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
            => GenericResult<T>.Success((T)(IGenericService)new SimpleService());
    }

    [ExcludeFromCodeCoverage]
    private class TestProvider : IFdwServiceProvider
    {
        public Task<IGenericResult<T>> Get<T>(string name, CancellationToken cancellationToken = default) where T : IGenericService
            => Task.FromResult(GenericResult<T>.Failure(new GenericMessage("Not implemented")));

        public Task<IGenericResult<T>> Get<T>(Guid id, CancellationToken cancellationToken = default) where T : IGenericService
            => Task.FromResult(GenericResult<T>.Failure(new GenericMessage("Not implemented")));

        public Task<IGenericResult<IReadOnlyList<T>>> Get<T>(CancellationToken cancellationToken = default) where T : IGenericService
            => Task.FromResult(GenericResult<IReadOnlyList<T>>.Failure(new GenericMessage("Not implemented")));
    }

    [ExcludeFromCodeCoverage]
    private class TestServiceType : ServiceTypeBase<SimpleService, SimpleFactory, SimpleConfig>
    {
        public TestServiceType()
            : base("Test", "TestSection", "Test", "Test",
                   "Test")
        {
        Registration((builder, loggerFactory) =>
        {
                return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        }

    }

    

}
