using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AspNetCoreRateLimit;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MMSConstants;
using Serilog;
using Tev.API.AuthPolicies;
using Tev.API.Configuration;
using Tev.API.Mocks;
using Tev.API.Models;
using Tev.API.Service;
using Tev.Cosmos;
using Tev.Cosmos.Entity;
using Tev.Cosmos.IRepository;
using Tev.Cosmos.Repository;
using Tev.DAL;
using Tev.DAL.HelperService;
using Tev.DAL.RepoConcrete;
using Tev.DAL.RepoContract;
using Tev.IotHub;
using ZohoSubscription;

namespace Tev.API
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
            ////needed to load configuration from appsettings
            services.AddOptions();

            ////needed to store ratelimit counter and ip rules
            services.AddMemoryCache();

            ////read the configuration value string, build a new configuration by the json stream
            var ipRateLimitingStr = RequestRateLimitTEVConfig.GetRateLimitConfig();
            var ipRateLimitingConfiguration = new ConfigurationBuilder().AddJsonStream(new MemoryStream(Encoding.ASCII.GetBytes(ipRateLimitingStr))).Build();

            ////load general configuration 
            services.Configure<IpRateLimitOptions>(ipRateLimitingConfiguration);

            //// inject counter and rules stores
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

            
            services.AddHsts(options =>
            {
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            #region Cors Configuration
            services.AddCors(options =>
            {
                options.AddPolicy("TevWeb",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
            });
            #endregion

            services.AddControllers().AddJsonOptions(
                opts =>
                {
                    opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                }).ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = actionContext =>
                    {
                        return InvalidModelCustomResponse.CustomErrorResponse(actionContext);
                    };
                });
            services.AddApiVersioning(config=>
            {
                config.ReportApiVersions = true;
                config.DefaultApiVersion = new ApiVersion(1, 0);
                config.AssumeDefaultVersionWhenUnspecified = true;
            });
            services.AddScoped<IMockData, MockData>();
            services.AddSingleton<IMemoryCache, MemoryCache>();

            #region Zoho Configuration

            var zohoSection = Configuration.GetSection("Zoho");

                services.AddHttpClient<IZohoAuthentication, ZohoAuthentication>(client =>
                {
                    client.BaseAddress = new Uri(zohoSection.GetValue<string>("accessTokenEndPoint"));
                    client.DefaultRequestHeaders.Accept.Clear();
                });

                services.AddHttpClient<IZohoSubscription, ZohoSubscription.ZohoSubscription>(client =>
                {
                    client.BaseAddress = new Uri(zohoSection.GetValue<string>("baseUrl"));
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Add("X-com-zoho-subscriptions-organizationid", zohoSection.GetValue<string>("OrgId"));
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                });

                services.AddHttpClient<IZohoService, ZohoService>(client =>
                {
                    client.BaseAddress = new Uri(zohoSection.GetValue<string>("baseUrl"));
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Add("X-com-zoho-subscriptions-organizationid", zohoSection.GetValue<string>("OrgId"));
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                });

            #endregion

            #region Sql Configuration
            services.AddDbContext<AppDbContext>(opt => 
            {
                var connection = Configuration.GetSection("Sql").GetValue<string>("ConnectionString");
                opt.UseSqlServer(connection);
                
            },ServiceLifetime.Scoped);

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepo<>), typeof(GenericRepo<>));
            services.AddScoped<IUserDevicePermissionService, UserDevicePermissionService>();
            #endregion

           

            services.AddSingleton<ITevIoTRegistry, TevIoTRegistry>(provider =>
            {
                return new TevIoTRegistry(Configuration.GetValue<string>("IoTHubConString"));
            });

            // Add cosmos db service as singleton, since we want to resuse same cosmos client across all request
            services.AddSingleton<ICosmosDbService<Alert>>(CosmosConfig<Alert>.InitializeCosmosClientInstanceAsync(Configuration.GetSection("CosmosDb"), 
                Configuration.GetValue<string>("CosmosDb:TevViolationTelemetryContainerName")).GetAwaiter().GetResult());
            services.AddSingleton<ICosmosDbService<AlertCount>>(CosmosConfig<AlertCount>.InitializeCosmosClientInstanceAsync(Configuration.GetSection("CosmosDb"), 
                Configuration.GetValue<string>("CosmosDb:TevViolationTelemetryContainerName")).GetAwaiter().GetResult());
            services.AddSingleton<ICosmosDbService<DeviceSetup>>(CosmosConfig<DeviceSetup>.InitializeCosmosClientInstanceAsync(Configuration.GetSection("CosmosDb"), 
                Configuration.GetValue<string>("CosmosDb:deviceSetupContainer")).GetAwaiter().GetResult());
            services.AddSingleton<ICosmosDbService<WSDSummaryEntity>>(CosmosConfig<WSDSummaryEntity>.InitializeCosmosClientInstanceAsync(Configuration.GetSection("CosmosDb"),
                Configuration.GetValue<string>("CosmosDb:TevViolationTelemetryContainerName")).GetAwaiter().GetResult());
            services.AddSingleton<ICosmosDbService<PeopleCount>>(CosmosConfig<PeopleCount>.InitializeCosmosClientInstanceAsync(Configuration.GetSection("CosmosDb"),
               Configuration.GetValue<string>("CosmosDb:TevViolationTelemetryContainerName")).GetAwaiter().GetResult());
            services.AddSingleton<ICosmosDbService<FirmwareUpdateHistory>>(CosmosConfig<FirmwareUpdateHistory>.InitializeCosmosClientInstanceAsync(Configuration.GetSection("CosmosDb"),
                Configuration.GetValue<string>("CosmosDb:deviceFirmwareHistoryContainerName")).GetAwaiter().GetResult());
            services.AddSingleton<ICosmosDbService<Device>>(CosmosConfig<Device>.InitializeCosmosClientInstanceAsync(Configuration.GetSection("CosmosDb"),
             Configuration.GetValue<string>("CosmosDb:deviceContainerName")).GetAwaiter().GetResult());

            services.AddSingleton<IAlertRepo, AlertRepo>(provider =>
            {
                return new AlertRepo(provider.GetService<ICosmosDbService<Alert>>());
            });
            services.AddSingleton<IGeneralRepo, GeneralRepo>(provider =>
            {
                return new GeneralRepo(provider.GetService<ICosmosDbService<AlertCount>>());
            });
            services.AddSingleton<IDeviceSetupRepo, DeviceSetupRepo>(provider =>
            {
                return new DeviceSetupRepo(provider.GetService<ICosmosDbService<DeviceSetup>>());
            });
            services.AddSingleton<IWSDSummaryAlertRepo, WSDSummaryAlertRepo>(provider =>
            {
                return new WSDSummaryAlertRepo(provider.GetService<ICosmosDbService<WSDSummaryEntity>>());
            });
            services.AddSingleton<IPeopleCountRepo, PeopleCountRepo>(provider =>
            {
                return new PeopleCountRepo(provider.GetService<ICosmosDbService<PeopleCount>>());
            });
            services.AddSingleton<IFirmwareUpdateHistoryRepo, FirmwareUpdateHistoryRepo>(provider =>
            {
                return new FirmwareUpdateHistoryRepo(provider.GetService<ICosmosDbService<FirmwareUpdateHistory>>());
            });
            services.AddSingleton<IDeviceRepo, DeviceRepo>(provider =>
            {
                return new DeviceRepo(provider.GetService<ICosmosDbService<Device>>());
            });

            var identityServerUrl = Configuration.GetValue<string>("IdentityServer");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Authority should be set to identity server base url
                options.Authority = identityServerUrl;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    RequireAudience = false,
                    ValidateAudience = false
                };
                var validator = options.SecurityTokenValidators.OfType<JwtSecurityTokenHandler>().SingleOrDefault();

                // Turn off Microsoft's JWT handler that maps claim types to .NET's long claim type names
                validator.InboundClaimTypeMap = new Dictionary<string, string>();
                validator.OutboundClaimTypeMap = new Dictionary<string, string>();
            });

            // Add custom role authorization
            services.AddAuthorization(options => {

                options.AddPolicy("OrgAdmin|SiteAdmin", policy =>
                {
                    policy.Requirements.Add(new RoleRequirement(new List<string> { MMSRoles.OrgAdmin, MMSRoles.SiteAdmin }));

                });

                options.AddPolicy("OrgAdmin", policy =>
                {
                    policy.Requirements.Add(new RoleRequirement(new List<string> { MMSRoles.OrgAdmin }));

                });

                options.AddPolicy("IMPACT", policy => policy.RequireClaim("application", "TEV"));

            });


            // Swagger section
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "TEV APIs",
                });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath, true);
                options.OperationFilter<SwaggerAuthHeader>();
                options.CustomSchemaIds(type => type.ToString());
                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        Password = new OpenApiOAuthFlow
                        {
                            TokenUrl = new Uri(new Uri(identityServerUrl), "/connect/token"),
                            RefreshUrl = new Uri(new Uri(identityServerUrl), "/connect/token")
                        }
                    },
                });
            });

            // Add firebase cloud messaging
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GetFirebaseCredentials(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
            });


            // configuration (resolvers, counter key builders)
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseIpRateLimiting();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.Use(async (context, next) =>
            {
                if (context.Request.Method == "HEAD")
                {
                    context.Response.StatusCode = 405;
                    return;
                }
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                foreach (var cookieKey in context.Request.Cookies.Keys)
                {
                    context.Response.Cookies.Delete(cookieKey);
                }
                await next();
            });
            app.UseSwagger();
            app.UseSwaggerUI(setupAction =>
            {
                setupAction.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                setupAction.RoutePrefix = string.Empty;
                setupAction.InjectStylesheet("swagger-custom.css");
                setupAction.DefaultModelsExpandDepth(-1);
                setupAction.OAuthClientId("tev.webui");

            });
            app.UseRouting();
            app.UseCors("TevWeb");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSerilogRequestLogging();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //this function automatically migrate database when application start
            MigrateSqlDB(app);
           
        }

        private static void MigrateSqlDB(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices
                                          .GetRequiredService<IServiceScopeFactory>()
                .CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetService<AppDbContext>())
                {
                    context.Database.Migrate();
                }
            }
        }

        private static GoogleCredential GetFirebaseCredentials(string env)
        {
            if (env == "Production")
                return GoogleCredential.FromFile(Path.Combine("tev-production-firebase.json"));
            else
                return GoogleCredential.FromFile(Path.Combine("tev-dev-firebase.json"));
        }
    }
}
