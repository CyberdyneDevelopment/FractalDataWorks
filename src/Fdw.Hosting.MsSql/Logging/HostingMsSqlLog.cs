using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Hosting.MsSql.Logging;

/// <summary>
/// MessageLogging for FDW MsSql hosting operations.
/// EventId range: 520-540.
/// </summary>
// Why: 520–527 retired with ControlDb purge (ControlDbConfigured/Connected/ConnectionFailed,
// StartupSecretManagerCreated/Failed, MsSqlConfigurationSourceAdded, MsSqlDataStoreTypeRegistered,
// MsSqlConfigurationWriterBackendRegistered). EventIds not reused.
[ExcludeFromCodeCoverage(Justification = "MessageLogging partial class - implementation is source-generated")]
public static partial class HostingMsSqlLog
{
}
