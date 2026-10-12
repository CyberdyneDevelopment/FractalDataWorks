using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests;

public class DataAbstractionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanLoadAssembly()
    {
        var assembly = typeof(IDataFormat).Assembly;
        assembly.ShouldNotBeNull();
        assembly.GetName().Name.ShouldBe("Fdw.Data.Abstractions");
    }
}
