using System.Threading.Tasks;

namespace Fdw.SignalR.Tests.Doubles;

/// <summary>
/// Minimal strongly-typed client interface used to exercise <see cref="Fdw.SignalR.RealTimeHubBase{TClient}"/>.
/// </summary>
public interface ITestHubClient
{
    /// <summary>A no-op client callback used only to satisfy the typed-hub generic constraint.</summary>
    Task Ping();
}
