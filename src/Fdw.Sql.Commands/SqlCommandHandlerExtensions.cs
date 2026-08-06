using Fdw.Sql.Commands.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Sql.Commands;

/// <summary>DI registration helper.</summary>
public static class SqlCommandHandlerExtensions
{
    public static IServiceCollection AddSqlCommandHandler(this IServiceCollection services)
    {
        services.AddSingleton<ISqlTranslatorRegistry, SqlTranslatorRegistry>();
        services.AddSingleton<ISqlCommandHandler, SqlCommandHandler>();
        return services;
    }
}
