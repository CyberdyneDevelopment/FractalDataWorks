using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>What a kind needs from the endpoint that calls it: the platform settings (the limits on a calculation) and the logger.</summary>
/// <param name="Settings">Supplies the limits on a calculation.</param>
/// <param name="Logger">The logger a refusal is written to.</param>
public sealed record TransformationKindContext(IEffectiveSettingsProvider Settings, ILogger Logger);
