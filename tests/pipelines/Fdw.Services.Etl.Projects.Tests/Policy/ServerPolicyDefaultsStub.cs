using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Tests.Policy;

/// <summary>
/// A server-defaults provider whose one current row holds the given values: what the resolver and the validators read
/// the server's policy defaults from (FDW-3580).
/// </summary>
internal static class ServerPolicyDefaultsStub
{
    public static Mock<IServerPolicyDefaultConfigurationProvider> Returning(
        string stepFailurePolicy = "HaltStage",
        string stageFailurePolicy = "HaltProject",
        int maxParallelPipelines = 5,
        bool requireApprovalToRun = true,
        bool allowResume = false,
        bool allowCrossTenant = false,
        Guid? resiliencyPolicyId = null)
    {
        var row = new ServerPolicyDefaultConfiguration
        {
            Name = "Default",
            Domain = "ServerPolicyDefault",
            Implementation = "ServerPolicyDefault",
            StepFailurePolicy = stepFailurePolicy,
            StageFailurePolicy = stageFailurePolicy,
            MaxParallelPipelines = maxParallelPipelines,
            RequireApprovalToRun = requireApprovalToRun,
            AllowResume = allowResume,
            AllowCrossTenant = allowCrossTenant,
            ResiliencyPolicyId = resiliencyPolicyId,
        };

        var provider = new Mock<IServerPolicyDefaultConfigurationProvider>();
        provider.Setup(p => p.GetCurrent(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(GenericResult<IServerPolicyDefaultImplementationConfiguration>.Success(row)));
        return provider;
    }

    public static Mock<IServerPolicyDefaultConfigurationProvider> Failing()
    {
        var provider = new Mock<IServerPolicyDefaultConfigurationProvider>();
        provider.Setup(p => p.GetCurrent(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(GenericResult<IServerPolicyDefaultImplementationConfiguration>.Failure(
                new Fdw.Messages.GenericMessage("the ServerPolicyDefault domain holds 0 current rows"))));
        return provider;
    }
}
