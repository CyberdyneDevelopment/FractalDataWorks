using Fdw.ServiceTypes;

namespace Fdw.Services.Messaging.Tests;

/// <summary>
/// The messaging limits a host gets when it changes none and states none (FDW-7058, FDW-7059, FDW-7103): the option's declared defaults, resolved the way
/// registration resolves them, so a test never writes the number itself.
/// </summary>
internal static class MessagingDefaults
{
    /// <summary>The default of one limit, by its key.</summary>
    /// <param name="field">The limit's key, such as <c>MaxPictureBytes</c>.</param>
    /// <returns>The number.</returns>
    public static int Of(string field) => DeclaredSetting.Count(new DefaultMessagingServiceType(), field, null).Value;
}
