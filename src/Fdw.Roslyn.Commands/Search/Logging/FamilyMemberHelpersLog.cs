using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Search.Logging;

/// <summary>
/// MessageLogging methods for FamilyMemberHelpers static helpers.
/// EventId range: 9140-9169.
/// </summary>
[MessageLoggingTypeCode("SEARCH")]
public static partial class FamilyMemberHelpersLog
{
    /// <summary>Trace: starting public-member enumeration for a type.</summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Trace,
        Message = "GetDeclaredPublicMembers start type={typeName}")]
    public static partial IGenericMessage GetDeclaredPublicMembersStart(ILogger logger, string typeName);

    /// <summary>Trace: a member was skipped because of an enumeration filter.</summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Trace,
        Message = "Member skipped type={typeName} member={memberName} reason={reason}")]
    public static partial IGenericMessage MemberSkipped(ILogger logger, string typeName, string memberName, string reason);

    /// <summary>Trace: a member passed all filters and is yielded.</summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Trace,
        Message = "Member included type={typeName} member={memberName} kind={kind}")]
    public static partial IGenericMessage MemberIncluded(ILogger logger, string typeName, string memberName, string kind);

    /// <summary>Trace: computing a method-shaped signature key.</summary>
    [MessageLogging(EventId = 11021, Level = LogLevel.Trace,
        Message = "GetMemberKey method name={methodName} paramCount={paramCount} key='{key}'")]
    public static partial IGenericMessage GetMemberKeyMethod(ILogger logger, string methodName, int paramCount, string key);

    /// <summary>Trace: computing a property/field/event signature key (name only).</summary>
    [MessageLogging(EventId = 11022, Level = LogLevel.Trace,
        Message = "GetMemberKey non-method name='{name}'")]
    public static partial IGenericMessage GetMemberKeyOther(ILogger logger, string name);

    /// <summary>Trace: ToContractMember called.</summary>
    [MessageLogging(EventId = 11023, Level = LogLevel.Trace,
        Message = "ToContractMember member={memberName} isAbstract={isAbstract} isVirtual={isVirtual} isStatic={isStatic} isOverride={isOverride}")]
    public static partial IGenericMessage ToContractMemberCalled(ILogger logger, string memberName, bool isAbstract, bool isVirtual, bool isStatic, bool isOverride);

    /// <summary>Trace: DescribeTypeParameter called.</summary>
    [MessageLogging(EventId = 11024, Level = LogLevel.Trace,
        Message = "DescribeTypeParameter name={paramName} constraintCount={constraintCount}")]
    public static partial IGenericMessage DescribeTypeParameterCalled(ILogger logger, string paramName, int constraintCount);

    /// <summary>Trace: DescribeTypeKind returned a label.</summary>
    [MessageLogging(EventId = 11025, Level = LogLevel.Trace,
        Message = "DescribeTypeKind type={typeName} kind={kind}")]
    public static partial IGenericMessage DescribeTypeKindResult(ILogger logger, string typeName, string kind);

    /// <summary>Trace: DerivesFrom starting with an interface root.</summary>
    [MessageLogging(EventId = 11026, Level = LogLevel.Trace,
        Message = "DerivesFrom interface root={rootName} candidate={typeName}")]
    public static partial IGenericMessage DerivesFromInterfaceCheck(ILogger logger, string typeName, string rootName);

    /// <summary>Trace: DerivesFrom starting with a class root, walking base chain.</summary>
    [MessageLogging(EventId = 11027, Level = LogLevel.Trace,
        Message = "DerivesFrom class-chain root={rootName} candidate={typeName}")]
    public static partial IGenericMessage DerivesFromClassChain(ILogger logger, string typeName, string rootName);

    /// <summary>Trace: DerivesFrom final result.</summary>
    [MessageLogging(EventId = 11028, Level = LogLevel.Trace,
        Message = "DerivesFrom result={derives} root={rootName} candidate={typeName}")]
    public static partial IGenericMessage DerivesFromResult(ILogger logger, string typeName, string rootName, bool derives);

    /// <summary>Trace: starting BFS enumeration of all named types in a compilation.</summary>
    [MessageLogging(EventId = 11029, Level = LogLevel.Trace,
        Message = "EnumerateAllNamedTypes start assembly={assemblyName}")]
    public static partial IGenericMessage EnumerateAllNamedTypesStart(ILogger logger, string assemblyName);

    /// <summary>Debug: finished BFS — count of types yielded.</summary>
    [MessageLogging(EventId = 11030, Level = LogLevel.Debug,
        Message = "EnumerateAllNamedTypes done assembly={assemblyName} typeCount={typeCount}")]
    public static partial IGenericMessage EnumerateAllNamedTypesDone(ILogger logger, string assemblyName, int typeCount);
}
