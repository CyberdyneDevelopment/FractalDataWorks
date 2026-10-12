using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Search.Logging;

/// <summary>
/// MessageLogging methods for InspectFamilyContractTranslator.
/// EventId range: 9180-9199.
/// </summary>
[MessageLoggingTypeCode("SEARCH")]
public static partial class InspectFamilyContractTranslatorLog
{
    /// <summary>Info: Translate called.</summary>
    [MessageLogging(EventId = 11055, Level = LogLevel.Information,
        Message = "InspectFamilyContract.Translate start typeName={typeName}")]
    public static partial IGenericMessage TranslateStart(ILogger logger, string typeName);

    /// <summary>Warning: TypeName parameter was empty.</summary>
    [MessageLogging(EventId = 21003, Level = LogLevel.Warning,
        Message = "InspectFamilyContract validation failed: TypeName required")]
    public static partial IGenericMessage ValidationFailedTypeNameRequired(ILogger logger);

    /// <summary>Warning: type could not be resolved.</summary>
    [MessageLogging(EventId = 31004, Level = LogLevel.Warning,
        Message = "InspectFamilyContract type not found: {typeName}")]
    public static partial IGenericMessage TypeNotFound(ILogger logger, string typeName);

    /// <summary>Trace: type resolved.</summary>
    [MessageLogging(EventId = 11056, Level = LogLevel.Trace,
        Message = "InspectFamilyContract resolved type={fullName} kind={kind} isAbstract={isAbstract} isSealed={isSealed}")]
    public static partial IGenericMessage TypeResolved(ILogger logger, string fullName, string kind, bool isAbstract, bool isSealed);

    /// <summary>Trace: enumerated generic parameters.</summary>
    [MessageLogging(EventId = 11057, Level = LogLevel.Trace,
        Message = "InspectFamilyContract genericParameterCount={count}")]
    public static partial IGenericMessage GenericParametersEnumerated(ILogger logger, int count);

    /// <summary>Trace: walked base type chain.</summary>
    [MessageLogging(EventId = 11058, Level = LogLevel.Trace,
        Message = "InspectFamilyContract baseTypeChainCount={count}")]
    public static partial IGenericMessage BaseChainWalked(ILogger logger, int count);

    /// <summary>Trace: enumerated directly-implemented interfaces.</summary>
    [MessageLogging(EventId = 11059, Level = LogLevel.Trace,
        Message = "InspectFamilyContract directInterfaceCount={count}")]
    public static partial IGenericMessage InterfacesEnumerated(ILogger logger, int count);

    /// <summary>Debug: enumerated public members of the contract.</summary>
    [MessageLogging(EventId = 11060, Level = LogLevel.Debug,
        Message = "InspectFamilyContract publicMemberCount={count}")]
    public static partial IGenericMessage MembersEnumerated(ILogger logger, int count);

    /// <summary>Information: Translate completed successfully.</summary>
    [MessageLogging(EventId = 11061, Level = LogLevel.Information,
        Message = "InspectFamilyContract.Translate success typeName={typeName} memberCount={memberCount}")]
    public static partial IGenericMessage TranslateSuccess(ILogger logger, string typeName, int memberCount);
}
