using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using AspNet.Security.OAuth.Vkontakte;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Models;
using Sisbi.Hubs;
using Sisbi.Services;
using Sisbi.Services.Contracts;
using Sisbi.Settings;
using Twilio;

namespace Sisbi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        public IConfiguration Configuration { get; }
        readonly string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<SisbiContext>(options =>
                options.UseLazyLoadingProxies().UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));

            services.Configure<KestrelServerOptions>(options => { options.Limits.MaxRequestBodySize = int.MaxValue; });

            services.AddSignalR();

            services.AddCors(options =>
            {
                options.AddPolicy(MyAllowSpecificOrigins,
                    builder =>
                    {
                        builder
                            .WithOrigins("null", "localhost", "89.16.101.3", "http://89.16.101.3", "http://89.16.101.3:5000")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
            });

            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue;
                x.MultipartHeadersLengthLimit = int.MaxValue;
            });

            var authConfig = Configuration.GetSection("Authentication");

            services.AddHttpClient<VkontakteService>()
                .ConfigurePrimaryHttpMessageHandler(() =>
                    new HttpClientHandler
                    {
                        AllowAutoRedirect = true,
                        UseDefaultCredentials = true
                    });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "http://localhost:5000",
                        ValidAudience = "http://localhost:5000",
                        ClockSkew = TimeSpan.Zero,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("superSecretKey@345"))
                    };
                });

            services
                .AddControllers()
                .AddJsonOptions(o =>
                {
                    //o.JsonSerializerOptions.IgnoreNullValues = true;
                    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo {Title = "Sisbi", Version = "v1"}); });

            services.Configure<TwilioSettings>(Configuration.GetSection("TwilioSettings"));
            services.Configure<EmailSettings>(Configuration.GetSection("EmailSettings"));

            services.AddTransient<IEmailService, EmailService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sisbi v1"));
            }

            ConfigureTwilio();

            //app.UseHttpsRedirection();
            app.UseRouting();
            app.UseStaticFiles();
            Console.WriteLine(env.WebRootPath);
            app.UseCors(MyAllowSpecificOrigins);
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<ResponseHub>("/response_hub");
            });
        }

        private void ConfigureTwilio()
        {
            var accountSid = Configuration["TwilioSettings:AccountSid"];
            var authToken = Configuration["TwilioSettings:AuthToken"];

            TwilioClient.Init(accountSid, authToken);
        }
    }
}