using FABModels.Model;
using FABTenantApi.Classes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Rotativa.AspNetCore;
using FABTenantD2Api.Classes;
using AspNetCoreRateLimit;
using FABTenantD2Api.Repository.DashboardRepo;
using FABTenantD2Api.DAL;

namespace FABTenantD2Api
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
            services.AddCors(options =>
            {
                var allowedOrigins = Configuration.GetSection("AllowedOrigins").Get<string[]>();
                options.AddPolicy("AllowSpecificOrigin", builder =>
                {
                    builder
                        .WithOrigins(allowedOrigins) // Replace with your Flutter app's URL or web app
                        .AllowAnyMethod()   // Allow any HTTP method
                        .AllowAnyHeader();  // Allow any header
                });
            });

            //Vendor Used
            //services.AddCors(
            //   option =>
            //   {
            //       option.AddPolicy("AllowAll", policy =>
            //       {
            //           policy.AllowAnyOrigin()
            //           .AllowAnyHeader()
            //           .AllowAnyMethod();
            //       });
            //   }
            //   );
            //DecoderReplacementFallback above with below
            //services.AddCors();
            services.AddDbContext<FABAppDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("FABModels")));
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<ITokenService, TokenService>();
            services.AddTransient<ITenantRepository, TenantRepository>();
            services.AddTransient<INotificationRepository, NotificationRepository>();
            services.AddTransient<IDashboardRepo, DashboardRepo>();
            services.AddTransient<IDBConnectionLogic, DBConnectionLogic>();
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.AddMvc().AddSessionStateTempDataProvider();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(15);//We set Time here 
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            services.AddAuthentication(auth =>
            {
                auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                //auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = Configuration["Jwt:Issuer"],
                    ValidAudience = Configuration["Jwt:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"])),
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "FABTenantApi", Version = "v1" });
                c.ResolveConflictingActions(x => x.First());
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Bearer",
                    BearerFormat = "JWT",
                    Scheme = "bearer",
                    Description = "Specify the authorization token.",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                    });
            });
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHsts(options =>
            {
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });
            // configure the resolvers
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            // app.UseEnableRequestRewind();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSession();
                app.UseSwaggerAuthorized();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FABTenantApi v1");
                    c.InjectStylesheet("/Content/css/myswagger.css");
                    c.InjectJavascript("/Content/js/myswagger.js");
                    c.DocumentTitle = "FAB Tenant Api";
                });
                app.UseExceptionHandler("/error-development");
            }
            else
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSession();
                app.UseSwaggerAuthorized();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FABTenantApi v1");
                    c.InjectStylesheet("/Content/css/myswagger.css");
                    c.InjectJavascript("/Content/js/myswagger.js");
                    c.DocumentTitle = "FAB Tenant Api";
                });
                app.UseExceptionHandler("/error");
            }
            // Shows UseCors with CorsPolicyBuilder.
            app.UseCors(builder =>
            {
                builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
            });

            RotativaConfiguration.Setup(env);
            app.UseStatusCodePagesWithReExecute("/{0}");
            app.UseHttpsRedirection();

            //app.UseAntiXssMiddleware(); 
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            //need to remove for idel time testing deadlock 10-02-2023
            //app.UseSlidingExpiration();

            //app.UseMiddleware<AutoLogMiddleWare>();
           // app.UseMiddleware<ApiKeyMiddleware>();
            app.UseStatusCodePages();
            app.UseMiddleware<EncryptionDecryptionMiddleware>();
            app.Use(async (context, next) =>
            {
                var token = context.Session.GetString("Token");
                if (!string.IsNullOrEmpty(token))
                {
                    context.Request.Headers.Add("Authorization", "Bearer " + token);
                }
                context.Response.Headers.Remove("Server"); 
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("X-Xss-Protection", "1");
                context.Response.Headers.Add("Referrer-Policy", "no-referrer");
                context.Response.Headers.Add("X-Permitted-Cross-Domain-Policies", "none");
                context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
                context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("Permissions-Policy", "accelerometer=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
                await next();
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                   name: "default",
                   pattern: "{controller=Default}/{action=Index}/{id?}");
                //endpoints.MapControllers();
            });
        }
    }
}
