using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc.Versioning;
// using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;

using SAWSCore3API.Helpers;
using SAWSCore3API.Services;
using SAWSCore3API.DBModels;
using SAWSCore3API.Authentication;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace MySqlCore3API
{
    public class Startup
    {
        readonly string DefaultCorsPolicy = "DefaultCorsPolicy";
        public IConfiguration Configuration { get; }

        //  private IWebHostEnvironment Environment;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
           //swagger service
            services.AddApiVersioning(config =>
            {
                config.DefaultApiVersion = new ApiVersion(1, 0);
                config.ReportApiVersions = true;
                config.AssumeDefaultVersionWhenUnspecified = true;


                config.ApiVersionReader = new QueryStringApiVersionReader("api-version");
                SwaggerConfig.UseQueryStringApiVersion("api-version");

            });

            services.AddSingleton<IUriService>(o =>
            {
                var accessor = o.GetRequiredService<IHttpContextAccessor>();
                var request = accessor.HttpContext.Request;
                var uri = string.Concat(request.Scheme, "://", request.Host.ToUriComponent() + request.PathBase);
                return new UriService(uri);
            });

            //services.AddControllers();
            services.AddControllers().AddJsonOptions(jsonOptions =>
            {
                jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            // MySQL Entity Framework
            //**** Firt time setup Migrations to create identity tables ****/
            //dotnet ef migrations add initPomeloIdentity -c ApplicationDbContext
            //dotnet ef database update
            services.AddDbContext<ApplicationDbContext>(options => options
                                                            .UseSqlServer(Configuration.GetConnectionString("ConnStr"))
                                                          );
            //services.AddDbContextPool<ApplicationDbContext>(options =>
            //{
            //    options.UseSqlServer(
            //        "ConnStr",
            //        options => options.EnableRetryOnFailure(
            //            maxRetryCount: 10,
            //            maxRetryDelay: System.TimeSpan.FromSeconds(360),
            //            errorNumbersToAdd: null)
            //        );
            //});


            // add/setup Identity  
            //services.AddIdentity<ApplicationUser, IdentityRole>()
            //    .AddEntityFrameworkStores<ApplicationDbContext>()
            //    .AddDefaultTokenProviders();

            services.AddIdentity<ApplicationUser, IdentityRole>(config =>
            {
                // User defined password policy settings.  
                config.Password.RequiredLength = 4;
                config.Password.RequireDigit = false;
                config.Password.RequireNonAlphanumeric = false;
                config.Password.RequireUppercase = false;
            }).AddRoles<IdentityRole>()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();


            // add/setup Authentication via JWT tokens
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })

            // Adding Jwt Bearer  
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = Configuration["JWT:ValidAudience"],
                    ValidIssuer = Configuration["JWT:ValidIssuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JWT:Secret"]))
                };
            });

            //Add Cors
            //services.AddCors();

            //services.AddCors(options =>
            //{
            //    options.AddDefaultPolicy(builder =>
            //    {
            //        builder.AllowAnyOrigin()
            //               .AllowAnyHeader()
            //               .AllowAnyMethod();
            //    });

            //    options.AddPolicy(name: "DefaultCorsPolicy",
            //        builder =>
            //        {
            //            builder.AllowAnyOrigin()
            //                   .AllowAnyHeader()
            //                   .AllowAnyMethod();
            //        });
            //});
            services.AddCors(options =>
            {
                options.AddPolicy(DefaultCorsPolicy,
                builder =>
                {
                    builder.SetIsOriginAllowed(isOriginAllowed: _ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                });
            });

            //Add httpclient to call 3rd party APIs
            services.AddHttpClient();

            //Add swagger documentation
            //services.AddSwaggerGen();
            services.AddSwaggerGen(setup =>
            {
                //setup.SwaggerDoc("v1", new OpenApiInfo { Title = "Jcred API", Version = "v1" });

                setup.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo() { Title = "Api version  1", Version = "v1", Description = "V1 Description", });
                setup.SwaggerDoc("v2", new Microsoft.OpenApi.Models.OpenApiInfo() { Title = "Api version  2", Version = "v2", Description = "V2 Description", });
                /// options.OperationFilter<AddAcceptHeaderParameter>();
                setup.OperationFilter<SwaggerParameterFilters>();
                setup.DocumentFilter<SwaggerVersionMapping>();

                setup.DocInclusionPredicate((version, desc) =>
                {
                    if (!desc.TryGetMethodInfo(out MethodInfo methodInfo)) return false;
                    var versions = methodInfo.DeclaringType.GetCustomAttributes(true).OfType<ApiVersionAttribute>().SelectMany(attr => attr.Versions);
                    var maps = methodInfo.GetCustomAttributes(true).OfType<MapToApiVersionAttribute>().SelectMany(attr => attr.Versions).ToArray();
                    version = version.Replace("v", "");
                    return versions.Any(v => v.ToString() == version && maps.Any(v => v.ToString() == version));
                });


                var jwtSecurityScheme = new OpenApiSecurityScheme
                {
                    BearerFormat = "JWT",
                    Name = "JWT Authentication",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",

                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };

                setup.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

                setup.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { jwtSecurityScheme, Array.Empty<string>() }
                });

            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                app.UseSwagger(options => options.RouteTemplate = "swagger/{documentName}/swagger.json");
                app.UseSwaggerUI(options =>
                {
                    //options.SwaggerEndpoint("./v1/swagger.json", "Jcred API v1");
                    options.DocumentTitle = "SAWS API Documentation";
                    options.SwaggerEndpoint($"/swagger/v1/swagger.json", $"SAWS API v1");
                    options.SwaggerEndpoint($"/swagger/v2/swagger.json", $"SAWS API v2");
                });
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            //always enable cors AFTER routing 
            //app.UseCors(builder =>
            //{
            //    builder
            //    .AllowAnyOrigin()
            //    .AllowAnyMethod()
            //    .AllowAnyHeader();
            //});
            app.UseCors(DefaultCorsPolicy);

            app.UseAuthentication();//Add authentication pipeline
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });


            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(env.ContentRootPath,  @"Uploads")),
                RequestPath = "/Uploads"
            });

        }

        
    }
}
