using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Execution;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Results;
using Fdw.Services.TokenManagers.Abstractions;

namespace Fdw.Services.Authentication.Tests.Flow;

/// <summary>A step that does whatever the test tells it to, including things it should not.</summary>
internal sealed class HostileStep : IAuthenticationStep
{
    public string Name { get; init; } = "Hostile";

    public IReadOnlyList<ContextElement> Requires { get; init; } = [];

    public IReadOnlyList<ContextElement> Contributes { get; init; } = [];

    public IReadOnlyList<string> AuthenticationMethods { get; init; } = [];

    public Func<AuthenticationContext, StepOutcome> Behaviour { get; init; } = _ => new StepOutcome.NotApplicable("nothing configured");

    public Task<IGenericResult<StepOutcome>> Execute(AuthenticationContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult<StepOutcome>.Success(Behaviour(context)));
}

/// <summary>
/// The steps a test's flow can name. In production the collection answers this — an option joined
/// it by being referenced — but the runner's invariants are about what it does with a step's own
/// declarations, so proving them needs steps that differ per case.
/// </summary>
internal sealed class NamedSteps
{
    private readonly Dictionary<string, IAuthenticationStep> _steps = new(StringComparer.Ordinal);

    public NamedSteps Add(string name, IAuthenticationStep step)
    {
        _steps[name] = step;
        return this;
    }

    /// <summary>Reads as the runner reads the collection: a name in, a step or nothing out.</summary>
    public IAuthenticationStep? Lookup(string stepName)
        => _steps.TryGetValue(stepName, out var s) ? s : null;
}

/// <summary>Counts methods. Two or more is "strong", one is "weak", none is null.</summary>
internal sealed class CountingAcrPolicy : IAcrPolicy
{
    public string? Evaluate(IReadOnlyList<string> achievedMethods) => achievedMethods.Count switch
    {
        0 => null,
        1 => "weak",
        _ => "strong",
    };

    public bool Meets(string? achieved, string? required)
        => required is null
        || (required == "weak" && achieved is "weak" or "strong")
        || (required == "strong" && achieved == "strong");
}

/// <summary>Records what it was asked to mint, so a test can inspect it.</summary>
internal sealed class RecordingIssuer : ITokenIssuer
{
    public IssuanceRequest? LastRequest { get; private set; }

    public int IssueCount { get; private set; }

    public Task<IGenericResult<IssuedToken>> Issue(IssuanceRequest request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        IssueCount++;
        return Task.FromResult(GenericResult<IssuedToken>.Success(new IssuedToken
        {
            AccessToken = "test-token",
            TokenType = "Bearer",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
        }));
    }
}

/// <summary>Single-use by TryRemove, so consuming is atomic rather than check-then-act.</summary>
internal sealed class InMemoryExecutions : IAuthenticationExecutionStore
{
    private readonly ConcurrentDictionary<string, ExecutionRecord> _records = new(StringComparer.Ordinal);
    private int _next;

    public Task<IGenericResult<string>> Suspend(ExecutionRecord record, CancellationToken cancellationToken = default)
    {
        var token = $"resume-{Interlocked.Increment(ref _next)}";
        _records[token] = record;
        return Task.FromResult(GenericResult<string>.Success(token));
    }

    public Task<IGenericResult<ExecutionRecord>> TryConsume(string resumeToken, CancellationToken cancellationToken = default)
        => Task.FromResult(_records.TryRemove(resumeToken, out var r)
            ? GenericResult<ExecutionRecord>.Success(r)
            : GenericResult<ExecutionRecord>.Failure(ServicesResultCodes.ByName("ConfigurationNotFound")));
}
