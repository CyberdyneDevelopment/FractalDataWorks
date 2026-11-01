namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// A permission gate an administrator can place in a flow and point at a permission, without editing its settings by hand.
/// </summary>
/// <remarks>
/// <para>
/// Opt-in, and separate from <see cref="IPermissionGatedAuthenticationStep"/> so a gate can exist without
/// it: every gate says which permission its settings require, but only a gate that also writes those
/// settings can be administered from a console. A flow gated by a step that does not implement this is
/// shown with its permission and changed in configuration.
/// </para>
/// <para>
/// <see cref="IAuthenticationFlowAdministration"/> uses the one step registered with this interface. With
/// none, or several, it limits no flow, because choosing between gates is not its decision to make.
/// </para>
/// </remarks>
public interface IAdministrablePermissionGate : IPermissionGatedAuthenticationStep
{
    /// <summary>Writes the settings that make this step require <paramref name="permission"/>.</summary>
    /// <param name="permission">The permission a principal must hold.</param>
    /// <returns>The step row's Configuration column, as <see cref="IPermissionGatedAuthenticationStep.RequiredPermission"/> reads it back.</returns>
    /// <remarks>Only the step knows the shape of its own settings, so it writes them.</remarks>
    string ConfigurationRequiring(string permission);
}
