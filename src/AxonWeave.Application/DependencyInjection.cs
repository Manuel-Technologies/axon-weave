using Microsoft.Extensions.DependencyInjection;

namespace AxonWeave.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
