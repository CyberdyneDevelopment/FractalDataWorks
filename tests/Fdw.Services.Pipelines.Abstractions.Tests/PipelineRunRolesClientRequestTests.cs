using System.Text.Json;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Abstractions.Tests;

/// <summary>
/// The roles a run acts with belong to the run (FDW-1161): a trigger names them, a pipeline carries none. The API proxy reaches
/// the ETL host through the client requests, so the roles must survive the proxy's read of the body and the client's write of
/// it, none may be chosen on the caller's behalf, and a pipeline's own requests state none.
/// </summary>
public sealed class PipelineRunRolesClientRequestTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TriggerRequestCarriesRunRolesThroughTheProxy()
    {
        var read = JsonSerializer.Deserialize<TriggerPipelineRequest>("""{"name":"P","runRoles":["Admin","Operator"]}""", Web);

        var sent = JsonSerializer.Serialize(read, Web);

        read!.RunRoles.ShouldNotBeNull().ShouldBe(new[] { "Admin", "Operator" });
        sent.ShouldContain("\"runRoles\":[\"Admin\",\"Operator\"]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NoRunRoleIsChosenForTheCaller()
    {
        new TriggerPipelineRequest().RunRoles.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void APipelinesOwnRequestsStateNoRunRoles()
    {
        JsonSerializer.Serialize(new CreatePipelineClientRequest(), Web).ShouldNotContain("runRoles");
        JsonSerializer.Serialize(new UpdatePipelineClientRequest(), Web).ShouldNotContain("runRoles");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ARunRolesMemberSentToAPipelineIsNotRead()
    {
        var read = JsonSerializer.Deserialize<UpdatePipelineClientRequest>("""{"description":"x","runRoles":["Admin"]}""", Web);

        JsonSerializer.Serialize(read, Web).ShouldNotContain("runRoles");
    }
}
