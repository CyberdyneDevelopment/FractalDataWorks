using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Tests that set <c>APP_SLOT_FILE</c> or a placeholder's environment variable change the process's environment, which every
/// schema load reads, so they run alone.
/// </summary>
[CollectionDefinition(nameof(SlotFileEnvironmentCollection), DisableParallelization = true)]
public sealed class SlotFileEnvironmentCollection
{
}
