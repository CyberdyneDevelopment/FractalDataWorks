using Shouldly;
using Xunit;

namespace Fdw.Data.DataContainers.Abstractions.Tests;

public class DataContainerAbstractionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanLoadAssembly()
    {
        var assembly = typeof(IDataRow).Assembly;
        assembly.ShouldNotBeNull();
        assembly.GetName().Name.ShouldBe("Fdw.Data.DataContainers.Abstractions");
    }
}
