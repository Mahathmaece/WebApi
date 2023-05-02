using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WebApi.Helpers;
using WebApi.Services;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using System.Security.Claims;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.OpenApi.Models;

namespace WebApi
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

            services.AddCors();
            //services.AddDbContext<DataContext>(x => x.UseInMemoryDatabase("TestDb"));
            services.AddDbContext<DataContext>(x => x.UseSqlServer(Configuration.GetConnectionString("sqlConnection")));
            services.AddControllers();
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            // configure strongly typed settings objects
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            // configure jwt authentication
            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                        var userId = int.Parse(context.Principal.Identity.Name);
                        var user = userService.GetById(userId);
                        if (user == null)
                        {
                            // return unauthorized if user no longer exists
                            context.Fail("Unauthorized");
                        }

                        string sessionID = context.Principal.Claims.Where(c => c.Type == CustomClaimTypes.AMSSessionID).FirstOrDefault().Value;
                        if(!userService.CheckSessionValidity(sessionID))
                        {
                            // session don't exist or expired
                            context.Fail("Session invalid");
                        }

                        return Task.CompletedTask;
                    }
                };
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            // set JsonConvert to use camel case (first letter is small case. same as .NET Core default)
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            services.AddSwaggerGen();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Implement Swagger UI",
                    Description = "A simple example to Implement Swagger UI",
                });
            });

            // configure DI for application services
            services.AddScoped<IUserService, UserService>();

            services.AddScoped<ICostCenterService, CostCenterService>();
            services.AddScoped<ICompanyService, CompanyService>();
            services.AddScoped<IBusinessAreaService, BusinessAreaService>();

            services.AddScoped<IMailService, MailService>();

            services.AddScoped<ILocationService, LocationService>();


            services.AddScoped<IUserRoleService, UserRoleService>();
            services.AddScoped<IUserRightService, UserRightService>();

            services.AddScoped<IModuleService, ModuleService>();
            services.AddScoped<IModulePageService, ModulePageService>();


            services.AddLogging(loggingBuilder => {
                //loggingBuilder.AddFile("Log/{0:yyyy}-{0:MM}-{0:dd}.log", fileLoggerOpts => {
                //    fileLoggerOpts.FormatLogFileName = fName => {
                //        return String.Format(fName, DateTime.UtcNow);
                //    };
                //});
                //var loggingSection = Configuration.GetSection("Logging");
                //loggingBuilder.AddFile(loggingSection);

                var loggingFileSection = Configuration.GetSection("Logging").GetSection("File");
                string logFolder = loggingFileSection.GetSection("Path").Value;
                if (String.IsNullOrEmpty(logFolder))
                    logFolder = "Log\\";

                bool append;
                if (!bool.TryParse(loggingFileSection.GetSection("Append").Value, out append))
                    append = true;

                string minLevel = "Information";
                if (String.IsNullOrEmpty(loggingFileSection.GetSection("MinLevel").Value))
                    minLevel = "Information";  // if not, specified use Information

                long fileSizeLimitBytes;
                if (!long.TryParse(loggingFileSection.GetSection("FileSizeLimitBytes").Value, out fileSizeLimitBytes))
                    fileSizeLimitBytes = 0;   // if not, put 0, means no limit to log file size

                int maxRollingFiles;
                if (!int.TryParse(loggingFileSection.GetSection("MaxRollingFiles").Value, out maxRollingFiles))
                    maxRollingFiles = 0;   // this is additional setting for fileSizeLimitBytes's setting. Put 0, means no limit of no. of rolling files.

                loggingBuilder.AddFile(logFolder + "{0:yyyy}-{0:MM}-{0:dd}.log", fileLoggerOpts =>
                {
                    fileLoggerOpts.Append = append;
                    LogLevel minLevelEnum;
                    switch (minLevel)
                    {
                        case "Trace":
                            minLevelEnum = LogLevel.Trace;
                            break;
                        case "Debug":
                            minLevelEnum = LogLevel.Debug;
                            break;
                        case "Information":
                            minLevelEnum = LogLevel.Information;
                            break;
                        case "Warning":
                            minLevelEnum = LogLevel.Warning;
                            break;
                        case "Error":
                            minLevelEnum = LogLevel.Error;
                            break;
                        case "Critical":
                            minLevelEnum = LogLevel.Critical;
                            break;
                        case "None":
                            minLevelEnum = LogLevel.None;
                            break;
                        default:
                            minLevelEnum = LogLevel.None;
                            break;
                    }
                    fileLoggerOpts.MinLevel = minLevelEnum;
                    fileLoggerOpts.FileSizeLimitBytes = fileSizeLimitBytes;
                    fileLoggerOpts.MaxRollingFiles = maxRollingFiles;
                    fileLoggerOpts.FormatLogFileName = fName =>
                    {
                        return String.Format(fName, DateTime.UtcNow);
                    };
                });

            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHttpsRedirection();

            app.UseRouting();

            // global cors policy
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSwagger();
            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Showing API V1");
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
