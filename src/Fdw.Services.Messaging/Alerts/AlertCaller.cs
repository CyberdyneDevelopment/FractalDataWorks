using System;
using System.Collections.Generic;

namespace Fdw.Services.Messaging;

/// <summary>
/// Who is acting on an alert, taken from the authentication context (FDW-2420).
/// </summary>
/// <param name="UserId">The signed-in person.</param>
/// <param name="Permissions">The permissions the person holds, which decide whether an action that needs one is theirs to take.</param>
internal readonly record struct AlertCaller(Guid UserId, IReadOnlyList<string> Permissions);
