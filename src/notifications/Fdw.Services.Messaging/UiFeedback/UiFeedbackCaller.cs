using System;
using System.Collections.Generic;
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
/// <param name="Writes">Whether the person holds the write right, so may leave any comment and comment on any they see; without it they may only ask questions
/// and comment on the questions they asked or were asked.</param>
/// <param name="OrganizationId">The organization the person acts in, or <see langword="null"/>; the holders of a question's answering permission there are told
/// of it as well as the tenant's.</param>
/// <param name="Permissions">The permissions of the person's session, which say whether a question was put to them.</param>
internal readonly record struct UiFeedbackCaller(
    Guid UserId, Guid TenantId, ExecutionTenantScope Scope, bool SeesAll, bool Writes, Guid? OrganizationId, IReadOnlyCollection<string> Permissions);
