using Fdw.Services.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Etl.Execution.AtOnceRecovery;

/// <summary>Reads the half-done rule a pipeline states, through the pipeline domain's provider, as the tenant of the run (FDW-6986).</summary>
internal sealed class PipelineHalfDonePipelines : IHalfDonePipelines
{
    private readonly IServiceScopeFactory _scopes;

    /// <summary>Initializes a new instance of the <see cref="PipelineHalfDonePipelines"/> class.</summary>
    /// <param name="scopes">Gives each call a scope of its own.</param>
    public PipelineHalfDonePipelines(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<string?>> RuleOf(Guid tenantId, string pipelineName, CancellationToken cancellationToken = default)
    {
        var scope = _scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var accessor = scope.ServiceProvider.GetRequiredService<IAuthenticationContextAccessor>();
            if (accessor.Current is null)
            {
                accessor.Current = new SystemAuthenticationContext(tenantId);
            }

            var found = await scope.ServiceProvider.GetRequiredService<IPipelineConfigurationProvider>().Get(pipelineName, ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, cancellationToken).ConfigureAwait(false);
            if (found.IsFailure || found.Value is null)
                return found.ToNewResult<string?>();

            return GenericResult<string?>.Success((found.Value as IHalfDoneRulePipelineConfiguration)?.HalfDoneRule);
        }
    }
}
