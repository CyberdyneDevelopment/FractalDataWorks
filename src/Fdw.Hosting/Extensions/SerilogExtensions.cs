using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Serilog;

namespace Fdw.Hosting.Extensions;

public static class SerilogExtensions
{
    /// <summary>Default flush timeout applied by <see cref="FlushFrameworkSerilog"/>.</summary>
    public static readonly TimeSpan DefaultFlushTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Bounded teardown for Serilog. Equivalent to <c>Log.CloseAndFlushAsync()</c> but never blocks
    /// longer than <paramref name="timeout"/>. Use in <c>finally</c> blocks instead of the raw
    /// Serilog call to keep one slow / unreachable sink (Loki, Seq, OTLP, etc.) from hanging the
    /// shutdown path indefinitely (FDW-424).
    /// </summary>
    /// <remarks>
    /// The Grafana Loki sink in particular blocks <c>Dispose()</c> on a TCP connect attempt to
    /// the configured endpoint; when the endpoint is unreachable the dispose can sit for the
    /// full OS TCP timeout. That happened during SCH-17 / ETL-16 triage and masked the actual
    /// fatal exception thrown above. This wrapper bounds the wait and lets the process exit
    /// even when a sink is misbehaving. Events queued in the sink at timeout are dropped —
    /// acceptable trade for guaranteed teardown.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public static async Task FlushFrameworkSerilog(TimeSpan? timeout = null)
    {
        var deadline = timeout ?? DefaultFlushTimeout;
        // Why: Log.CloseAndFlushAsync() returns ValueTask; wrap with AsTask so it composes with WhenAny.
        var closeTask = Log.CloseAndFlushAsync().AsTask();
        var winner = await Task.WhenAny(closeTask, Task.Delay(deadline)).ConfigureAwait(false);
        if (winner != closeTask)
        {
            // Why: we deliberately do not await closeTask here — that's the deadlock we're avoiding.
            // The underlying sink can finish flushing on its own thread; we just stop waiting.
            await Console.Error.WriteLineAsync(
                $"[FDW.Hosting] Serilog teardown exceeded {deadline.TotalSeconds:F1}s — proceeding with shutdown.")
                .ConfigureAwait(false);
        }
    }
}
