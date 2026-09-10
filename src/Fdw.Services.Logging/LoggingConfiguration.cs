using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Logging.Abstractions;

namespace Fdw.Services.Logging;

/// <summary>
/// The logging domain configuration: which logging implementation is configured, and its settings.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Logging")]
public partial class LoggingConfiguration : DomainConfigurationBase, ILoggingConfiguration
{






}
