using Fdw.Services.Connections.TestDouble;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-5959: a connection type states its DDL dialect, and one that states none (the base's default) is refused with a 400, never
/// given another engine's text. Asked of a concrete type that derives <c>ConnectionTypeBase</c> and overrides nothing about DDL: the
/// test double. (The registry is empty in the connections test assembly, so a loop over every registered type proved nothing there.)
/// </summary>
public class ConnectionTypeDdlDialectTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AConnectionTypeThatStatesNoDialectIsRefusedWith400()
    {
        var dialect = new MockConnectionType().DdlGenerator();

        dialect.IsFailure.ShouldBeTrue();
        ResultHttpStatusMapper.Map(dialect, new DefaultHttpContext()).StatusCode.ShouldBe(400);
    }
}
