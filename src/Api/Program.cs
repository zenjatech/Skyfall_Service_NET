using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Skyfall.Api;
using Skyfall.Api.Middleware;
using Skyfall.Infrastructure.Data;

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, config) =>
    {
        var env = context.HostingEnvironment.EnvironmentName;
        config.SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
    })
    .ConfigureFunctionsWorkerDefaults(worker =>
    {
        worker.UseMiddleware<CorsMiddleware>();
        worker.UseMiddleware<ExceptionHandlingMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationServices(context.Configuration);
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
