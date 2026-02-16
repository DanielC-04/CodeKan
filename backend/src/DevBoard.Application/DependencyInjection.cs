using Microsoft.Extensions.DependencyInjection;

namespace DevBoard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
