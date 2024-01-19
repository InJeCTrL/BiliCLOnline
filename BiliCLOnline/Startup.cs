using System;
using System.IO;
using BiliCLOnline.IServices;
using BiliCLOnline.Services;
using BiliCLOnline.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;


namespace BiliCLOnline
{
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
            var port = 80;
            var url = Configuration.GetValue<string>("urls") ?? "";
            if (url.Contains(':'))
            {
                int.TryParse(url[(url.LastIndexOf(':') + 1)..], out port);
            }

            if (Configuration.GetValue<bool>("LocalVersion"))
            {
                Helper.OpenURL($"http://localhost:{port}/index.html?localversion={port}");
            }
            services.AddSingleton<WebHelper>();
            services.AddSingleton<Helper>();
            services.AddScoped<IBearerInfo, BearerInfo>();
            services.AddScoped<IReplyResult, ReplyResult>();
            services.AddScoped<ILogin, Login>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "BiliCLOnline", Version = "v1" });
            });

            services.AddQueuePolicy(option =>
            {
                option.MaxConcurrentRequests = 10;
                option.RequestQueueLimit = 50;
            });

            services.AddCors(options =>
            {
                options.AddPolicy("cors", p =>
                {
                    if (!Configuration.GetValue<bool>("LocalVersion"))
                    {
                        p.WithOrigins(Environment.GetEnvironmentVariable("CorsTarget") ?? "")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                    }
                    else
                    {
                        p.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    }
                });
            });

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!Configuration.GetValue<bool>("LocalVersion"))
            {
                app.UseConcurrencyLimiter();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "HttpReplApi v1");
                });
            }

            app.UseCors("cors");

            if (!Configuration.GetValue<bool>("LocalVersion"))
            {
                app.UseMiddleware<HCaptchaVerifyingMiddleware>();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            if (Configuration.GetValue<bool>("LocalVersion"))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(
                        Path.Combine(env.ContentRootPath, "./web")),
                    RequestPath = ""
                });
            }
        }
    }
}
