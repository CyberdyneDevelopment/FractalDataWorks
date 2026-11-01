using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for an administrator changing who may use a sign-in flow.
/// </summary>
/// <remarks>EventId range: 91260–91269, continued at 91300–91308.</remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class FlowAdministrationLog
{
    /// <summary>Every configured flow was read for an administrator.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">How many flows are configured.</param>
    [MessageLogging(EventId = 91260, Level = LogLevel.Trace,
        Message = "Read {count} configured sign-in flows for administration")]
    internal static partial IGenericMessage FlowsRead(ILogger<AuthenticationFlowAdministration> logger, int count);

    /// <summary>The flow rows could not be read.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91261, Level = LogLevel.Error,
        Message = "The sign-in flow rows could not be read, so no flow can be administered")]
    internal static partial IGenericMessage FlowsUnreadable(ILogger<AuthenticationFlowAdministration> logger);

    /// <summary>No flow was named.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91262, Level = LogLevel.Error,
        Message = "A sign-in flow must be named to change it")]
    internal static partial IGenericMessage NameMissing(ILogger<AuthenticationFlowAdministration> logger);

    /// <summary>No flow of that name is configured.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">What was named.</param>
    [MessageLogging(EventId = 91263, Level = LogLevel.Warning,
        Message = "No sign-in flow named '{flowName}' is configured")]
    internal static partial IGenericMessage FlowNotConfigured(ILogger<AuthenticationFlowAdministration> logger, string flowName);

    /// <summary>A flow was switched on or off.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="isEnabled">Whether it is now on.</param>
    /// <param name="administrator">Who switched it.</param>
    [MessageLogging(EventId = 91264, Level = LogLevel.Information,
        Message = "Sign-in flow '{flowName}' switched {isEnabled} by {administrator}")]
    internal static partial IGenericMessage FlowSwitched(ILogger<AuthenticationFlowAdministration> logger, string flowName, string isEnabled, Guid administrator);

    /// <summary>A flow now requires a permission.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="permission">The permission.</param>
    /// <param name="administrator">Who limited it.</param>
    [MessageLogging(EventId = 91265, Level = LogLevel.Information,
        Message = "Sign-in flow '{flowName}' now admits only holders of '{permission}', set by {administrator}")]
    internal static partial IGenericMessage FlowGated(ILogger<AuthenticationFlowAdministration> logger, string flowName, string permission, Guid administrator);

    /// <summary>A flow is now open to every active user.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="administrator">Who opened it.</param>
    [MessageLogging(EventId = 91266, Level = LogLevel.Information,
        Message = "Sign-in flow '{flowName}' now admits every active user, opened by {administrator}")]
    internal static partial IGenericMessage FlowOpened(ILogger<AuthenticationFlowAdministration> logger, string flowName, Guid administrator);

    /// <summary>The change asked for is how the flow already stands, so nothing was written.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    [MessageLogging(EventId = 91267, Level = LogLevel.Debug,
        Message = "Sign-in flow '{flowName}' already stands as asked; nothing written")]
    internal static partial IGenericMessage FlowUnchanged(ILogger<AuthenticationFlowAdministration> logger, string flowName);

    /// <summary>The permission named is not defined.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="permission">What was named.</param>
    [MessageLogging(EventId = 91268, Level = LogLevel.Warning,
        Message = "Sign-in flow '{flowName}' was not limited to '{permission}': no permission of that name is defined")]
    internal static partial IGenericMessage PermissionNotDefined(ILogger<AuthenticationFlowAdministration> logger, string flowName, string permission);

    /// <summary>Nothing in the flow establishes what a permission gate reads.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="requires">What the gate needs.</param>
    [MessageLogging(EventId = 91269, Level = LogLevel.Warning,
        Message = "Sign-in flow '{flowName}' cannot require a permission: none of its steps establishes {requires}")]
    internal static partial IGenericMessage FlowCannotBeGated(ILogger<AuthenticationFlowAdministration> logger, string flowName, string requires);

    /// <summary>The flow requires several permissions, so no single one is changed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="count">How many gate steps it has.</param>
    [MessageLogging(EventId = 91300, Level = LogLevel.Warning,
        Message = "Sign-in flow '{flowName}' has {count} permission gates; change it in configuration, not one permission at a time")]
    internal static partial IGenericMessage FlowGateAmbiguous(ILogger<AuthenticationFlowAdministration> logger, string flowName, int count);

    /// <summary>This host does not have exactly one permission-gated step to place in a flow.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">How many are registered.</param>
    // Warning, not Error: a host that never limits flows by permission legitimately references no gate
    // package, and every flow listing would otherwise log an error. With several registered, the choice
    // between them is not this service's to make, so gating stays unavailable until one is removed.
    [MessageLogging(EventId = 91301, Level = LogLevel.Warning,
        Message = "{count} permission-gated authentication steps are registered on this host; exactly one is needed to limit a flow by permission")]
    internal static partial IGenericMessage GateStepUnavailable(ILogger<AuthenticationFlowAdministration> logger, int count);

    /// <summary>The changed flow could not be saved.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="reason">What the save reported.</param>
    [MessageLogging(EventId = 91302, Level = LogLevel.Error,
        Message = "Sign-in flow '{flowName}' could not be saved: {reason}")]
    internal static partial IGenericMessage FlowSaveFailed(ILogger<AuthenticationFlowAdministration> logger, string flowName, string? reason);

    /// <summary>No role carries the permission, so a flow limited to it would admit nobody.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="permission">The permission.</param>
    [MessageLogging(EventId = 91303, Level = LogLevel.Warning,
        Message = "Sign-in flow '{flowName}' was not limited to '{permission}': no role carries it, so the flow would admit nobody")]
    internal static partial IGenericMessage PermissionHeldByNoRole(ILogger<AuthenticationFlowAdministration> logger, string flowName, string permission);

    /// <summary>The change would leave the administrator no way to sign in the way they signed in now.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="administrator">Who asked for it.</param>
    /// <param name="methods">The methods their sign-in proved.</param>
    [MessageLogging(EventId = 91304, Level = LogLevel.Warning,
        Message = "Change to sign-in flow '{flowName}' refused: it would leave {administrator} no enabled flow proving {methods} that admits them")]
    internal static partial IGenericMessage ChangeLocksOutAdministrator(
        ILogger<AuthenticationFlowAdministration> logger, string flowName, Guid administrator, string methods);

    /// <summary>The flow's gate is a step this host cannot configure.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="stepName">The gate step it names.</param>
    [MessageLogging(EventId = 91305, Level = LogLevel.Warning,
        Message = "Sign-in flow '{flowName}' is limited by '{stepName}', which this host cannot configure; change it in configuration")]
    internal static partial IGenericMessage FlowGateNotAdministrable(ILogger<AuthenticationFlowAdministration> logger, string flowName, string stepName);

    /// <summary>The administrator did not sign in through a flow, so no flow change can lock them out.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="administrator">Who asked.</param>
    [MessageLogging(EventId = 91306, Level = LogLevel.Trace,
        Message = "{administrator} did not sign in through a flow; no flow change can lock them out")]
    internal static partial IGenericMessage NoFlowSignInToProtect(ILogger<AuthenticationFlowAdministration> logger, Guid administrator);

    /// <summary>The role grants could not be read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow a limit was asked for.</param>
    [MessageLogging(EventId = 91307, Level = LogLevel.Error,
        Message = "Sign-in flow '{flowName}' was not limited: the role grants could not be read to confirm anyone can hold the permission")]
    internal static partial IGenericMessage RoleGrantsUnreadable(ILogger<AuthenticationFlowAdministration> logger, string flowName);

    /// <summary>What the administrator holds could not be resolved.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="administrator">Who asked.</param>
    [MessageLogging(EventId = 91308, Level = LogLevel.Error,
        Message = "The permissions of {administrator} could not be resolved, so a restrictive sign-in change cannot be checked and is refused")]
    internal static partial IGenericMessage AdministratorPermissionsUnresolved(ILogger<AuthenticationFlowAdministration> logger, Guid administrator);
}
