using System;
using Fdw.Operations.Execution;

namespace Fdw.Services.Messaging;

/// <summary>
/// Who is asking and which tenants they reach, taken from the authentication context (FDW-2143). The person's name is not here: it is read from their account
/// when a service records it (FDW-2199).
/// </summary>
/// <param name="UserId">The signed-in person.</param>
/// <param name="TenantId">The tenant the person acts in.</param>
/// <param name="Scope">The tenants the person reaches.</param>
/// <param name="SeesAll">Whether the person holds the read right, so sees the comments of every urgency; otherwise they see the comments that are not security
/// issues and their own (FDW-2246).</param>
internal readonly record struct UiFeedbackCaller(Guid UserId, Guid TenantId, ExecutionTenantScope Scope, bool SeesAll);
