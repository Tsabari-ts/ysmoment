using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YsMoment.Core.Interfaces;
using YsMoment.Infrastructure.Data;
using YsMoment.Infrastructure.Services;

namespace YsMoment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            if (env.IsDevelopment())
                options.UseSqlite(configuration.GetConnectionString("Sqlite") ?? "Data Source=ysmoment.db");
            else
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsql =>
                    npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null));
        });

        services.AddScoped<IImageValidator, ImageValidator>();
        if (env.IsDevelopment())
            services.AddScoped<IImageStorageService, LocalImageStorageService>();
        else
            services.AddScoped<IImageStorageService, CloudinaryImageStorageService>();

        if (env.IsDevelopment())
            services.AddScoped<ISmsService, ConsoleSmsService>();
        else
            services.AddHttpClient<ISmsService, Sms4FreeService>();
        services.AddSingleton<SmsQueue>();
        services.AddSingleton<ISmsQueue>(sp => sp.GetRequiredService<SmsQueue>());
        services.AddHostedService<SmsBackgroundService>();

        services.AddScoped<EventService>();
        services.AddScoped<OrderService>();
        services.AddScoped<AuthService>();
        services.AddSingleton<VerificationCodeService>();

        return services;
    }
}
