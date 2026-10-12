using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>The notification of a Notify stage as the stage mapping reads it.</summary>
/// <param name="RuleName">The notification rule that sends.</param>
/// <param name="RunPoint">The run point the notification fires at.</param>
/// <param name="Message">The message template.</param>
public sealed record TransformNotificationSpec(string RuleName, string RunPoint, string Message) : INotificationSpec;
