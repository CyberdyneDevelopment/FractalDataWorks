using Fdw.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Resiliency.Abstractions;
using Fdw.Services.Resiliency.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Resiliency;

/// <summary>
/// Baseline <see cref="IResiliencyPolicyProvider"/>: reports no policies and no server default.
/// Satisfies DI for <see cref="ResiliencyExecutor"/> on systems that have not yet configured
/// resiliency policies. Callers that pass <c>null</c> as <c>policyId</c> to
/// <see cref="IResiliencyExecutor.Execute"/> bypass this provider entirely (pass-through).
/// </summary>
public sealed class EmptyResiliencyPolicyProvider : IResiliencyPolicyProvider
{
    private readonly ILogger<EmptyResiliencyPolicyProvider> _logger;

    /// <summary>Initializes a new instance of <see cref="EmptyResiliencyPolicyProvider"/>.</summary>
    public EmptyResiliencyPolicyProvider(ILogger<EmptyResiliencyPolicyProvider>? logger = null)
    {
        _logger = logger ?? NullLogger<EmptyResiliencyPolicyProvider>.Instance;
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IGenericConfiguration>> Get(Guid policyId, CancellationToken cancellationToken = default)
    {
        var msg = ResiliencyLog.PolicyNotFound(_logger, Guid.Empty, policyId.ToString("N"));
        IGenericResult<IGenericConfiguration> result = GenericResult<IGenericConfiguration>.Failure(msg);
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Guid? GetServerDefaultPolicyId() => null;
}
