using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using TtsApi.Authentication;
using TtsApi.Authentication.Policies;
using TtsApi.Authentication.Policies.Handler;
using TtsApi.Authentication.Policies.Requirements;
using TtsApi.BackgroundServices;
using TtsApi.ExternalApis.Discord;
using TtsApi.Hubs.TtsHub;
using TtsApi.Hubs.TtsHub.TransformationClasses;
using TtsApi.Model;

namespace TtsApi
{
    public class Startup
    {
        /// <summary>
        /// <c>TreatTinyAsBoolean=false</c> results in using bit(1) instead of tinyint(1) for <see cref="bool"/>.
        /// </summary>
        private const string AdditionalMySqlConfigurationParameters = ";TreatTinyAsBoolean=false";

        private IConfiguration Configuration { get; }
        private IHostEnvironment HostingEnvironment { get; }

        public Startup(IConfiguration configuration, IHostEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
            DiscordWebhook.SetWebhooks(Configuration.GetSection("DiscordWebhooks"));
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            //services.AddRouting(options => options.LowercaseUrls = true);
            services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo {Title = "TtsApi", Version = "v1"}); });

            services.AddDbContext<TtsDbContext>(opt =>
                opt.UseMySQL(Configuration.GetConnectionString("TtsDb") + AdditionalMySqlConfigurationParameters));

            //https://josef.codes/asp-net-core-protect-your-api-with-api-keys/
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = ApiKeyAuthenticationOptions.DefaultScheme;
                    options.DefaultChallengeScheme = ApiKeyAuthenticationOptions.DefaultScheme;
                })
                .AddApiKeySupport(_ => { });
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.RedemptionsScopes,
                    policy => policy.Requirements.Add(new RedemptionsScopesRequirements()));
                options.AddPolicy(Policies.CanChangeSettings,
                    policy => policy.Requirements.Add(new CanChangeSettingsRequirements()));
                options.AddPolicy(Policies.CanAccessQueue,
                    policy => policy.Requirements.Add(new CanAccessQueueRequirements()));
            });

            services.AddSingleton<IAuthorizationHandler, RedemptionsScopesHandler>();
            services.AddSingleton<IAuthorizationHandler, CanChangeSettingsHandler>();
            services.AddSingleton<IAuthorizationHandler, CanAccessQueueHandler>();


            services.AddCors(options =>
                {
                    options.AddDefaultPolicy(builder =>
                    {
                        if (HostingEnvironment.IsDevelopment())
                            builder.SetIsOriginAllowed(_ => true)
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                        else
                            builder.WithOrigins("https://*.icdb.dev")
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials()
                                .SetIsOriginAllowedToAllowWildcardSubdomains();
                    });
                }
            );
            services.AddSignalR();

            services.AddHostedService<IngestQueueHandler>();
            services.AddTransient<TtsHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TtsApi v1"));
            }

            app.UseExceptionHandler(appErr =>
                appErr.Run(context =>
                    {
                        context.Response.StatusCode = 500;
                        IExceptionHandlerPathFeature exception = context.Features.Get<IExceptionHandlerPathFeature>();
                        DiscordLogger.LogException(exception.Error);
                        return null;
                    }
                )
            );

            //app.UseHttpsRedirection(); //This breaks UseCors

            app.UseWebSockets();
            app.UseCors();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<TtsHub>("/TtsHub", options =>
                {
                    // Not sure if we even need this
                    // options.ApplicationMaxBufferSize = 30 * 1024; // * 1000;
                });
            });
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
