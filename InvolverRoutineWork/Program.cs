using Data.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        var connectionString = Environment.GetEnvironmentVariable("sqldb_connection");
        services.AddDbContext<DatabaseContext>(options =>
            options.UseSqlServer(connectionString));
    })
    .Build();

host.Run();
