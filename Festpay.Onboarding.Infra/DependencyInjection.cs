using System.Reflection;
using Carter;
using Festpay.Onboarding.Infra.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Festpay.Onboarding.Infra;

public static class DependencyInjection
{
    public static void AddDatabase(this IServiceCollection services)
    {
        services.AddDbContext<FestpayContext>();
        services.AddScoped(_ => new FestpayContextFactory().CreateDbContext());

        using var serviceProvider = services.BuildServiceProvider();
        using var context = serviceProvider.GetService<FestpayContext>();

        context?.Database.Migrate();
    }

    public static void AddSwagger(this IServiceCollection services, IConfiguration config)
    {
        services.AddSwaggerGen(c => c.LoadOpenApiOptions());
    }

    public static void AddCarterModules(this IServiceCollection services, Assembly applicationAssembly)
    {
        services.AddCarter(configurator: configurator =>
        {
            var moduleTypes = applicationAssembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(ICarterModule).IsAssignableFrom(t));

            configurator.WithModules(moduleTypes.ToArray());
        });
    }

    private static void LoadOpenApiOptions(this SwaggerGenOptions options)
    {
        var contact = new OpenApiContact() { Name = "Festpay Onboarding" };

        var info = new OpenApiInfo
        {
            Version = "v1",
            Title = "Festpay Onboarding",
            Description = "API designed to manage the Festpay Onboarding application.",
            Contact = contact,
        };

        options.SwaggerDoc("v1", info);
    }
}
