using Fdw.Collections;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// One option of <see cref="MessageRoles"/>: who a message in a conversation is from.
/// </summary>
public interface IMessageRole : ITypeOption<int, MessageRoleBase>
{
}
