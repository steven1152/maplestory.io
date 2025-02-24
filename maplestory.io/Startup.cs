﻿using maplestory.io.Data;
using maplestory.io.Entities;
using maplestory.io.Services.Implementations.MapleStory;
using maplestory.io.Services.Interfaces.MapleStory;
using maplestory.io.Services.Rethink;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace maplestory.io
{
    public class Startup
    {
        public static bool Ready;
        public static bool Started;
        public static string Hostname;
        public static string SHA1 = "Unknown";

        public Startup(IHostingEnvironment env)
        {
            Hostname = Dns.GetHostName();

            if (File.Exists("SHA1.hash"))
            {
                SHA1 = File.ReadAllText("SHA1.hash").Trim('\t', '\r', '\n', ' ');
                Console.WriteLine($"SHA1: {SHA1}");
            }

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddResponseCompression();

            services.AddRazorPages().AddRazorRuntimeCompilation();

            services.AddApplicationInsightsTelemetry();

            // Add framework services.
            services.AddMvc()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    options.SerializerSettings.Converters.Add(new ImageConverter());
                });

            services.AddCors();

            //services.Configure<RethinkDbOptions>(Configuration.GetSection("RethinkDb"));
            //services.AddSingleton<IRethinkDbConnectionFactory, RethinkDbConnectionFactory>();
            services.AddSingleton<IConfiguration>(Configuration);

            if (Configuration.GetChildren().FirstOrDefault(c => c.Key == "WZ") != null)
            {
                services.Configure<WZOptions>(Configuration.GetSection("WZ"));
                services.AddSingleton<IWZFactory, WZAppSettingsFactory>();
            }

            if (Environment.GetEnvironmentVariable("MYSQL_DBHOST") != null)
            {
                // ===== Add our DbContext ========
                services.AddDbContext<ApplicationDbContext>();

                // ===== Add Identity ========
                services.AddIdentity<IdentityUser, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequiredLength = 1;
                    options.Password.RequiredUniqueChars = 1;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                })
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();

                services.AddTransient<IWZFactory, WZFactory>();
            }
            services.AddTransient<IItemFactory, ItemFactory>();
            services.AddTransient<ISkillFactory, SkillFactory>();
            services.AddTransient<IMusicFactory, MusicFactory>();
            services.AddTransient<IMapFactory, MapFactory>();
            services.AddTransient<IMobFactory, MobFactory>();
            services.AddTransient<INPCFactory, NPCFactory>();
            services.AddTransient<IQuestFactory, QuestFactory>();
            services.AddTransient<ITipFactory, TipFactory>();
            services.AddTransient<IZMapFactory, ZMapFactory>();
            services.AddTransient<ICraftingEffectFactory, CraftingEffectFactory>();
            services.AddTransient<IAndroidFactory, AndroidFactory>();
            services.AddTransient<IPetFactory, PetFactory>();
            services.AddTransient<IAvatarFactory, AvatarFactory>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("V2", new OpenApiInfo { Title = "MapleStory.IO", Version = "V2", Contact = new OpenApiContact() { Email = "andy@crr.io", Name = "Andy", Url = new Uri("https://github.com/crrio/maplestory.io") }, Description = "The unofficial MapleStory API Documentation for developers." });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {

            ILogger logging = loggerFactory.CreateLogger<Startup>();

            app.UseResponseCompression();
            app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod());
            app.Use((ctx, next) =>
            {
                ctx.Response.OnStarting(state =>
                {
                    HttpContext realContext = (HttpContext)state;

                    realContext.Response.Headers.Add("X-Processed-By", $"{Hostname}/{SHA1}");

                    return Task.FromResult(0);
                }, ctx);

                return next();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // ===== Use Authentication ======
            app.UseAuthentication();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/V2/swagger.json", "MapleStory.IO V2");
            });
        }
    }
}
