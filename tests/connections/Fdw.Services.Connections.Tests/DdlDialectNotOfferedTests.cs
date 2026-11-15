using Fdw.Results;
using Fdw.Services.Connections.Results;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// FDW-5959: the refusal of a connection type that states no DDL dialect is a coded message that names the type (the refusal's 400
/// is proved in Fdw.Services.Data.Endpoints.Tests, where a concrete connection type is at hand). There is no default dialect.
/// </summary>
public class DdlDialectNotOfferedTests
{
    [Fact]
    public void TheReasonNamesTheConnectionType()
        => ConnectionsResultCodes.ByName("DdlDialectNotOffered")
            .FormatClientMessage(ResultDetails.Create("connectionType", "Http"))
            .ShouldBe("Connection type 'Http' offers no DDL dialect: it has no generator for its engine's DDL text");
}
