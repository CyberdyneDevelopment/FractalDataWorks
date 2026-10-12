using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.DevSession.Abstractions;
using Fdw.Results;

namespace Fdw.DevSession.Tests;

/// <summary>A stand-in engine used only to prove registration does not overwrite a host's choice.</summary>
/// <remarks>
/// Every member throws because nothing should ever call it — the test asserts identity, not
/// behaviour. A quietly-succeeding stub would let a future regression call this instead of the real
/// engine without any test noticing.
/// </remarks>
internal sealed class ThrowingWorktreeEngine : IWorktreeEngine
{
    public Task<IGenericResult<IsolatedCopy>> CreateBranch(IsolationRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Registration-only stand-in.");

    public Task<IGenericResult<IsolatedCopy>> CreateWorktree(IsolationRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Registration-only stand-in.");

    public Task<IGenericResult<string>> Commit(IsolatedCopy copy, string message, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Registration-only stand-in.");

    public Task<IGenericResult<string>> Push(IsolatedCopy copy, string remote, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Registration-only stand-in.");

    public Task<IGenericResult<string>> Merge(string repoPath, string sourceBranch, string targetBranch, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Registration-only stand-in.");

    public Task<IGenericResult<bool>> Remove(IsolatedCopy copy, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Registration-only stand-in.");
}
