using Microsoft.Extensions.Logging;
using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Configuration;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Fdw.Results;

namespace Fdw.ServiceTypes.Tests.TestDoubles;

/// <summary>
/// Concrete ServiceTypeBase implementation for testing.
/// Uses ITestService + TestServiceFactory + TestConfiguration.
/// </summary>
public sealed class TestServiceType : ServiceTypeBase<ITestService, TestServiceFactory, TestConfiguration>
{
    public TestServiceType()
        : base("TestType", "Services:TestType", "Test Type", "A test service type", "Testing")
    {
        Registration((builder, loggerFactory) =>
        {
        return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}

/// <summary>
/// Second concrete ServiceTypeBase with different generic arguments for ID determinism tests.
/// </summary>
public sealed class AlternateTestServiceType : ServiceTypeBase<ITestService, SimpleTestServiceFactory, IServiceConfiguration>
{
    public AlternateTestServiceType()
        : base("AlternateType", "Services:AlternateType", "Alternate Type", "An alternate test type", "Testing")
    {
    }

}

/// <summary>
/// Named ServiceTypeBase used to prove that two options which close the base identically — the shape
/// a real domain produces, where every option shares one service and one factory interface — are still
/// distinct members of their collection.
/// </summary>
public sealed class SameShapeServiceType : ServiceTypeBase<ITestService, TestServiceFactory, TestConfiguration>
{
    public SameShapeServiceType(string name)
        : base(name, "Services:" + name, name, "A test type closing the base the same way", "Testing")
    {
    }
}

/// <summary>
/// The same names through a different closure of the base, to show identity follows the name rather
/// than the generic arguments.
/// </summary>
public sealed class OtherShapeServiceType : ServiceTypeBase<ITestService, SimpleTestServiceFactory, IServiceConfiguration>
{
    public OtherShapeServiceType(string name)
        : base(name, "Services:" + name, name, "A test type closing the base a different way", "Testing")
    {
    }
}
