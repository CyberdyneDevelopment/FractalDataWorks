using System;

namespace Fdw.Services.Connections.Tests;

/// <summary>Stands for a command or a connection a script's record source owns (FDW-4903): counts how often it was released.</summary>
internal sealed class Released : IDisposable
{
    /// <summary>Gets how many times it was released.</summary>
    public int Count { get; private set; }

    public void Dispose() => Count++;
}
