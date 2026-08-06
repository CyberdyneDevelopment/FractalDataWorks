# CancellationToken Propagation

All async methods in FDW must accept and propagate `CancellationToken` so that callers can cancel long-running operations (HTTP requests, database queries, file I/O).

## Rule

**Every async method must accept `CancellationToken` and pass it to every async callee.**

## Interface Methods

```csharp
// CORRECT
Task<IGenericResult> TestConnection(CancellationToken cancellationToken = default);

// WRONG - missing CancellationToken
Task<IGenericResult> TestConnection();
```

Always include `/// <param name="cancellationToken">Cancellation token.</param>` in the XML docs.

## Implementation Methods

```csharp
// Public/interface implementations: use default parameter
public async Task<IGenericResult> TestConnection(CancellationToken cancellationToken = default)
{
    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
    // ...
}

// Private helpers: no default (caller must provide)
private async Task<IGenericResult> ExecuteInternal(CancellationToken cancellationToken)
{
    using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
    // ...
}

// Protected abstract/virtual: use default (subclass may call directly)
protected abstract Task<IGenericResult> PerformConnectionTest(CancellationToken cancellationToken = default);
```

## Propagation

Pass the token to **every** async callee:

```csharp
// CORRECT - propagated to all callees
public async Task<IGenericResult> Execute(CancellationToken cancellationToken = default)
{
    var connectResult = await Connect(cancellationToken).ConfigureAwait(false);
    using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
    {
        // ...
    }
    return result;
}

// WRONG - token available but not passed
public async Task<IGenericResult> Execute(CancellationToken cancellationToken = default)
{
    var connectResult = await Connect().ConfigureAwait(false);  // Missing CT!
    using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);  // Missing CT!
}
```

## Exceptions

These do **not** need `CancellationToken`:

| Category | Reason |
|----------|--------|
| SignalR hub client interfaces (`ICalculationHubClient`, etc.) | SignalR manages cancellation internally |
| Blazor event handlers (`HandleChange`, `HandleInput`) | UI framework manages lifecycle |
| Synchronous methods | No async operations to cancel |
| `Task.CompletedTask` returns | No actual async work |

## Naming

Always use the parameter name `cancellationToken` (not `ct`, `token`, or `cts`).
