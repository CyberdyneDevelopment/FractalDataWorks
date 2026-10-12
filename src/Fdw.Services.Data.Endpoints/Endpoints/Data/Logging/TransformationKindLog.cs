using System;
using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Logging;

/// <summary>
/// MessageLogging for choosing the kind of a transformation on the one <c>/transforms</c> resource (FDW-1221): a request that
/// names no kind, a kind nothing authors here, or a member that belongs to another kind.
/// </summary>
/// <remarks>EventIds follow the result categories (first digit of a five-digit id): 2 validation (400). This class uses 21560-21569.</remarks>
[MessageLoggingTypeCode("TRANSFORMKIND")]
public static partial class TransformationKindLog
{
    /// <summary>Logs a request that names no implementation.</summary>
    [MessageLogging(EventId = 21560, Level = LogLevel.Warning, Message = "A transformation request names no implementation; state Field or Built")]
    public static partial IGenericMessage ImplementationMissing(ILogger logger);

    /// <summary>Logs an implementation nothing authors through this resource.</summary>
    [MessageLogging(EventId = 21561, Level = LogLevel.Warning, Message = "Implementation '{implementation}' cannot be authored here; state Field or Built")]
    public static partial IGenericMessage ImplementationUnknown(ILogger logger, string implementation);

    /// <summary>Logs a member stated on a kind that does not own it.</summary>
    [MessageLogging(EventId = 21562, Level = LogLevel.Warning, Message = "Member '{member}' belongs to another kind of transformation; a {implementation} transformation does not take it")]
    public static partial IGenericMessage MemberNotForKind(ILogger logger, string implementation, string member);

    /// <summary>Logs a member a kind needs and the request leaves out.</summary>
    [MessageLogging(EventId = 21563, Level = LogLevel.Warning, Message = "A {implementation} transformation needs '{member}' and the request states none")]
    public static partial IGenericMessage MemberRequired(ILogger logger, string implementation, string member);
}
