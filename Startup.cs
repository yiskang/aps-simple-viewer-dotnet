using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        var clientID = Configuration["APS_CLIENT_ID"];
        var clientSecret = Configuration["APS_CLIENT_SECRET"];
        if (string.IsNullOrEmpty(clientID) || string.IsNullOrEmpty(clientSecret))
        {
            throw new ApplicationException("Missing required environment variables APS_CLIENT_ID or APS_CLIENT_SECRET.");
        }

        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = int.MaxValue;
            options.MultipartHeadersLengthLimit = int.MaxValue;
            options.ValueLengthLimit = int.MaxValue;
        });

        services.Configure<IISServerOptions>(options =>
        {
            options.MaxRequestBodySize = null;
        });
        services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = null;
        });

        services.AddControllers();
        services.AddSingleton<APS>(new APS(clientID, clientSecret));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
