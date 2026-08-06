namespace Fdw.DevSession.Abstractions;

/// <summary>
/// Describes a dev session to open: the fix/issue/conversation key it resolves and how to materialize
/// its isolated working copy. There are no silent defaults — the key, isolation request, and isolation
/// level name are all required and the manager fails loud when any is missing.
/// </summary>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class SessionRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionRequest"/> class.
    /// </summary>
    /// <param name="key">The key of the fix/issue/conversation the session resolves.</param>
    /// <param name="isolation">The request describing the repo, base ref, and branch to isolate.</param>
    /// <param name="isolationLevelName">The name of the <see cref="IIsolationLevel"/> strategy to materialize with.</param>
    public SessionRequest(string key, IsolationRequest isolation, string isolationLevelName)
    {
        Key = key;
        Isolation = isolation;
        IsolationLevelName = isolationLevelName;
    }

    /// <summary>Gets the key of the fix/issue/conversation the session resolves.</summary>
    public string Key { get; }

    /// <summary>Gets the request describing the repo, base ref, and branch to isolate.</summary>
    public IsolationRequest Isolation { get; }

    /// <summary>Gets the name of the <see cref="IIsolationLevel"/> strategy to materialize the copy with.</summary>
    public string IsolationLevelName { get; }
}
