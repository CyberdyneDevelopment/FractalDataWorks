using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Aui;
using Fdw.Results;

namespace Fdw.UI.WebMcp.Tests.Fakes;

/// <summary>
/// An <see cref="IAuiAction"/> that records what the WebMCP adapter handed it.
/// </summary>
public sealed class CapturingAuiAction : IAuiAction
{
    /// <inheritdoc />
    public string Name => "capture";

    /// <summary>Gets the parameters captured from the most recent execution.</summary>
    public IDictionary<string, object>? Captured { get; private set; }

    /// <summary>Gets the user id captured from the most recent execution.</summary>
    public Guid CapturedUserId { get; private set; }

    /// <inheritdoc />
    public Task<IGenericResult> Execute(
        Guid userId,
        IDictionary<string, object> parameters,
        CancellationToken ct = default)
    {
        Captured = parameters;
        CapturedUserId = userId;
        return Task.FromResult(GenericResult.Success());
    }
}
