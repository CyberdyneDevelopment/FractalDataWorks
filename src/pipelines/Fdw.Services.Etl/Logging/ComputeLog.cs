using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the Compute stage (FDW-1209): resolving the Built Transformation it applies when the run starts, and
/// refusing a record it cannot read. Every refusal is logged and returned in the failure result.
/// EventId range: 20601-20699.
/// </summary>
[MessageLoggingTypeCode("COMPUTE")]
public static partial class ComputeLog
{
    /// <summary>Logs that Compute stages cannot be resolved because the run has no Transformation provider.</summary>
    [MessageLogging(EventId = 20601, Level = LogLevel.Error,
        Message = "Stage '{stageName}' applies a Built transformation, but the host gave the run no Transformation provider to resolve it")]
    public static partial IGenericMessage TransformationProviderMissing(ILogger logger, string stageName);

    /// <summary>Logs that a Compute stage names no Transformation.</summary>
    [MessageLogging(EventId = 20602, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' states no Transformation to apply")]
    public static partial IGenericMessage TransformationNotStated(ILogger logger, string stageName);

    /// <summary>Logs a Transformation that no longer resolves.</summary>
    [MessageLogging(EventId = 20603, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' applies Transformation {transformationId}, which no longer resolves")]
    public static partial IGenericMessage TransformationMissing(ILogger logger, string stageName, Guid transformationId);

    /// <summary>Logs a Transformation that is not a Built transformation.</summary>
    [MessageLogging(EventId = 20604, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' applies Transformation '{transformationName}', a {implementation}, which is not a Built transformation")]
    public static partial IGenericMessage TransformationNotBuilt(ILogger logger, string stageName, string transformationName, string implementation);

    /// <summary>Logs an input the Built transformation declares that the stage maps no field to.</summary>
    [MessageLogging(EventId = 20605, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' maps no field to input '{inputName}' of Transformation '{transformationName}'")]
    public static partial IGenericMessage InputNotMapped(ILogger logger, string stageName, string inputName, string transformationName);

    /// <summary>Logs a mapped input the Built transformation does not declare.</summary>
    [MessageLogging(EventId = 20606, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' maps a field to '{inputName}', which Transformation '{transformationName}' does not declare as an input")]
    public static partial IGenericMessage InputNotDeclared(ILogger logger, string stageName, string inputName, string transformationName);

    /// <summary>Logs a Built transformation that does not compile.</summary>
    [MessageLogging(EventId = 20607, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' applies Transformation '{transformationName}', which cannot be compiled; the failure result names why")]
    public static partial IGenericMessage CalculationRefused(ILogger logger, string stageName, string transformationName);

    /// <summary>Logs that Compute stages cannot be compiled because the run has no settings provider.</summary>
    [MessageLogging(EventId = 20612, Level = LogLevel.Error,
        Message = "Stage '{stageName}' applies a Built transformation, but the host gave the run no settings provider to read the calculation limits from")]
    public static partial IGenericMessage SettingsProviderMissing(ILogger logger, string stageName);

    /// <summary>Logs that a required calculation limit setting is missing or not positive.</summary>
    [MessageLogging(EventId = 20608, Level = LogLevel.Error,
        Message = "The setting '{settingName}' that limits calculations is missing or not positive, so Compute stages cannot be compiled")]
    public static partial IGenericMessage LimitSettingMissing(ILogger logger, string settingName);

    /// <summary>Logs that a run compiled the calculations of its Compute stages.</summary>
    [MessageLogging(EventId = 20609, Level = LogLevel.Debug,
        Message = "Compiled {calculationCount} Compute calculation(s) for the run")]
    public static partial IGenericMessage CalculationsCompiled(ILogger logger, int calculationCount);

    /// <summary>Logs a Compute stage that reached a record with no calculation resolved for it.</summary>
    [MessageLogging(EventId = 20610, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' has no compiled calculation: the run did not resolve one")]
    public static partial IGenericMessage CalculationNotResolved(ILogger logger, string stageName);

    /// <summary>Logs a record that lacks the field a Compute input is mapped to.</summary>
    [MessageLogging(EventId = 20611, Level = LogLevel.Error,
        Message = "Compute stage '{stageName}' cannot read input '{inputName}': the record has no field '{sourceField}'")]
    public static partial IGenericMessage SourceFieldMissing(ILogger logger, string stageName, string inputName, string sourceField);
}
