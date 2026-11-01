using System;
using System.Runtime.InteropServices;

namespace Fdw.Services.Messaging;

/// <summary>
/// Who is choosing the actions of a kind of alert, taken from the authentication context (FDW-2430).
/// </summary>
/// <param name="UserId">The signed-in person.</param>
/// <param name="TenantId">The tenant the person acts in, the company the choice is for.</param>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct AlertKindCaller(Guid UserId, Guid TenantId);
