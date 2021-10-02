using System;
using System.IO;
using System.Reflection;
using Amazon.Polly;
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
using TtsApi.Authentication.Roles;
using TtsApi.BackgroundServices;
using TtsApi.ExternalApis.Aws;
using TtsApi.ExternalApis.Discord;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.CustomRewards;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.Redemptions;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub;
using TtsApi.ExternalApis.Twitch.Helix.Moderation;
using TtsApi.ExternalApis.Twitch.Helix.Users;
using TtsApi.Hubs.TtsHub;
using TtsApi.Hubs.TtsHub.TransformationClasses;
using TtsApi.Model;

namespace TtsApi
{
    public class Startup
    {
        /// <summary>
        /// <c>TreatTinyAsBoolean=false</c> results in using bit(1) instead of tinyint(1) for <see cref="bool"/>.<br/>
        /// <br/>
        /// In 5.0.5 SSL was enabled by default. It isn't necessary for our usage.
        /// (We don't expose the DB to the internet.)
        /// https://stackoverflow.com/a/45108611
        /// </summary>
        private const string AdditionalMySqlConfigurationParameters = ";TreatTinyAsBoolean=false;SslMode=none";

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
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TtsApi", Version = "v1" });

                // This is more or less a hotfix due to the way I currently run docker. 
                // Once I cleanup the docker file this won't be needed anymore.
                if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
                    c.IncludeXmlComments("TtsApi.xml");

                c.AddSecurityDefinition("OAuth", new OpenApiSecurityScheme
                {
                    Description = "Standard Twitch OAuth header. Example: \"OAuth 0123456789abcdefghijABCDEFGHIJ\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "OAuth"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string commentsFileName = Assembly.GetExecutingAssembly().GetName().Name + ".XML";
                string commentsFile = Path.Combine(baseDirectory, commentsFileName);
                if (File.Exists(commentsFile))
                    c.IncludeXmlComments(commentsFile);
            });

            services.AddDbContext<TtsDbContext>(opt =>
            {
                //Try env var first else use appsettings.json
                string dbConString = Environment.GetEnvironmentVariable(@"TTSAPI_CONNECTIONSTRINGS_DB");
                if (string.IsNullOrEmpty(dbConString))
                    dbConString = Configuration.GetConnectionString("TtsDb");
                opt.UseMySQL(dbConString + AdditionalMySqlConfigurationParameters);
            });

            //https://josef.codes/asp-net-core-protect-your-api-with-api-keys/
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = ApiKeyAuthenticationOptions.DefaultScheme;
                    options.DefaultChallengeScheme = ApiKeyAuthenticationOptions.DefaultScheme;
                })
                .AddApiKeySupport(_ => { });
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.CanChangeChannelSettings,
                    policy => policy.Requirements.Add(new CanChangeChannelSettingsRequirements()));
                options.AddPolicy(Policies.CanAccessQueue,
                    policy => policy.Requirements.Add(new CanAccessQueueRequirements()));
                options.AddPolicy(Policies.CanChangeBotSettings,
                    policy => policy.RequireRole(Roles.IrcBot, Roles.BotOwner, Roles.BotAdmin));
            });

            services.AddTransient<IAuthorizationHandler, CanChangeChannelSettingsHandler>();
            services.AddTransient<IAuthorizationHandler, CanAccessQueueHandler>();


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

            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());
            services.AddAWSService<IAmazonPolly>();
            services.AddSingleton<Polly>();
            services.AddHostedService<PrepareBotAndPrefetchData>();

            services.AddHostedService<IngestQueueHandler>();
            // Helix
            services.AddTransient<TtsHandler>();
            services.AddTransient<TtsAddRemoveHandler>();
            services.AddTransient<CustomRewards>();
            services.AddTransient<CustomRewardsRedemptions>();
            services.AddTransient<Moderation>();
            services.AddTransient<ModerationBannedUsers>();
            services.AddTransient<Users>();
            // EventSub
            services.AddTransient<Subscriptions>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "TtsApi v1");

                //Disable the "Try it out" button
                //c.SupportedSubmitMethods(Array.Empty<SubmitMethod>());

                //This garbage doesn't work and therefore the authorization is lost after every reload.
                //Making swagger completely useless for this project.
                //c.ConfigObject.AdditionalItems.Add("persistAuthorization", "true");
            });

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
