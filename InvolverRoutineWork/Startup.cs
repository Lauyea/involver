﻿using Data.Data;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

[assembly: FunctionsStartup(typeof(InvolverRoutineWork.Startup))]

namespace InvolverRoutineWork
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            string sqlConnection = Environment.GetEnvironmentVariable("sqldb_connection");

            builder.Services.AddDbContext<DatabaseContext>(
                options => options.UseSqlServer(sqlConnection));
        }
    }
}