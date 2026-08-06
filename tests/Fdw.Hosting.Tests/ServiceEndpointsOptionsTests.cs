using System;
using Fdw.Hosting.Configuration;
using Xunit;
using Shouldly;

namespace Fdw.Hosting.Tests;

public class ServiceEndpointsOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SectionNameIsServiceEndpoints()
    {
        ServiceEndpointsOptions.SectionName.ShouldBe("ServiceEndpoints");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SchedulerDefaultsToEmpty()
    {
        var options = new ServiceEndpointsOptions();
        options.Scheduler.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EtlDefaultsToEmpty()
    {
        var options = new ServiceEndpointsOptions();
        options.Etl.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SchedulerCanBeSet()
    {
        var options = new ServiceEndpointsOptions { Scheduler = "https://scheduler:5005" };
        options.Scheduler.ShouldBe("https://scheduler:5005");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EtlCanBeSet()
    {
        var options = new ServiceEndpointsOptions { Etl = "https://etl:5002" };
        options.Etl.ShouldBe("https://etl:5002");
    }
}
