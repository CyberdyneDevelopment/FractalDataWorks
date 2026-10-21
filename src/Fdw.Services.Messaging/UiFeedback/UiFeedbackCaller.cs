using System;
using Fdw.Operations.Execution;

namespace Fdw.Services.Messaging;

/// <summary>
/// Who is asking and which tenants they reach, taken from the authentication context (FDW-2143).
/// </summary>
/// <param name="UserId">The signed-in person.</param>
/// <param name="Name">The person's name.</param>
/// <param name="TenantId">The tenant the person acts in.</param>
/// <param name="Scope">The tenants the person reaches.</param>
internal readonly record struct UiFeedbackCaller(Guid UserId, string Name, Guid TenantId, ExecutionTenantScope Scope);
