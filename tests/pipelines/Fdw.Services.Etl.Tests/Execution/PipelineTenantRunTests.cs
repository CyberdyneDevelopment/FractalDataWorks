using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// A run's work acts as its tenant run (FDW-1087, FDW-1161): the pipeline's tenant and the roles the run was started with,
/// never the system context and never someone else. A run with no tenant, or whose roles cannot be
/// resolved, is refused.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class PipelineTenantRunTests
{
    private static readonly Guid TenantId = new("11111111-1111-1111-1111-111111111111");

    private static PipelineExecutionRequest Request(Guid? tenantId) => new()
    {
        ExecutionId = Guid.NewGuid(),
        PipelineId = Guid.NewGuid(),
        PipelineName = "NightlyLoad",
        TriggerSource = "Api",
        RunRoles = ["DataWriter"],
        TenantId = tenantId
    };

    private static ServiceProvider Services(Mock<IRoleGrants> roles)
    {
        var services = new ServiceCollection();
        services.AddSingleton(roles.Object);
        return services.BuildServiceProvider();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARunActsAsItsTenantWithItsOwnRolesNotAsTheSystem()
    {
        var request = Request(TenantId);
        var roles = new Mock<IRoleGrants>();
        roles.Setup(r => r.Get(request.RunRoles, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(["datasets:read", "datasets:write"]));

        var result = await PipelineExecutionBackgroundService.ResolveTenantRun(
            Services(roles), request, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var run = result.Value!;
        run.IsSystemContext.ShouldBeFalse();
        run.ActiveTenantId.ShouldBe(TenantId);
        run.PrincipalId.ShouldBe(request.PipelineId);
        run.Roles.ShouldBe(["DataWriter"]);
        run.Permissions.ShouldBe(["datasets:read", "datasets:write"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARunWithNoTenantIsRefusedAndNoRolesAreAskedFor()
    {
        var roles = new Mock<IRoleGrants>(MockBehavior.Strict);

        var result = await PipelineExecutionBackgroundService.ResolveTenantRun(
            Services(roles), Request(null), NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARunWhoseRolesCannotBeResolvedIsRefused()
    {
        var request = Request(TenantId);
        var roles = new Mock<IRoleGrants>();
        roles.Setup(r => r.Get(request.RunRoles, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Failure(new GenericMessage("the role no longer exists")));

        var result = await PipelineExecutionBackgroundService.ResolveTenantRun(
            Services(roles), request, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
