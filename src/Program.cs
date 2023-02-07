using BooksRHere.Models;
using BooksRHere.Objects;
using BooksRHere.Services;
using Couchbase.Lite;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebEssentials.AspNetCore.OutputCaching;
using WebMarkupMin.AspNetCore7;
using WebMarkupMin.Core;
using WilderMinds.MetaWeblog;

using IWmmLogger = WebMarkupMin.Core.Loggers.ILogger;
using Post = BooksRHere.Models.Post;
using WmmNullLogger = WebMarkupMin.Core.Loggers.NullLogger;

namespace BooksRHere
{
    public class Program
    {
        private static IConfiguration? _configuration;

        public static Database Db { get; private set; }

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            _configuration = builder?.Configuration;
            ConfigureServices(builder.Services);
            var app = builder.Build();
            var env = app.Environment;
            if (env is null)
            {
                throw new ArgumentNullException(nameof(env));
            }
            
            Db = new Database("BooksRHere", new DatabaseConfiguration() { Directory = Path.Combine(env.WebRootPath, "database") });
            Configure(app, env);
            app.Run();
        }

        /// <remarks>This method gets called by the runtime. Use this method to configure the HTTP request pipeline.</remarks>
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Shared/Error");
                app.UseHsts();
            }

            app.Use(
                (context, next) =>
                {
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    return next();
                });

            app.UseStatusCodePagesWithReExecute("/Shared/Error");
            app.UseWebOptimizer();

            app.UseStaticFilesWithCache();

            if (_configuration.GetValue<bool>("forcessl"))
            {
                app.UseHttpsRedirection();
            }

            app.UseMetaWeblog("/metaweblog");
            app.UseAuthentication();

            app.UseOutputCaching();
            app.UseWebMarkupMin();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapControllerRoute("default", "{controller=Blog}/{action=Index}/{id?}");
                });
        }

        /// <remarks>This method gets called by the runtime. Use this method to add services to the container.</remarks>
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddSingleton<IUserServices, BlogUserServices>();
            services.AddSingleton<IBlogService, FileBlogService>();
            services.AddSingleton<IDatabaseService<Post>, PostDatabaseService>();
            services.AddSingleton<IDatabaseService<Comment>, CommentDatabaseService>();
            services.Configure<BlogSettings>(_configuration.GetSection("blog"));
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddMetaWeblog<Services.MetaWeblogService>();

            // Progressive Web Apps https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker
            services.AddProgressiveWebApp(
                new WebEssentials.AspNetCore.Pwa.PwaOptions
                {
                    OfflineRoute = "/shared/offline/"
                });

            // Output caching (https://github.com/madskristensen/WebEssentials.AspNetCore.OutputCaching)
            services.AddOutputCaching(
                options =>
                {
                    options.Profiles["default"] = new OutputCacheProfile
                    {
                        Duration = 3600
                    };
                });

            // Cookie authentication.
            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(
                    options =>
                    {
                        options.LoginPath = "/login/";
                        options.LogoutPath = "/logout/";
                    });

            // HTML minification (https://github.com/Taritsyn/WebMarkupMin)
            services
                .AddWebMarkupMin(
                    options =>
                    {
                        options.AllowMinificationInDevelopmentEnvironment = true;
                        options.DisablePoweredByHttpHeaders = true;
                    })
                .AddHtmlMinification(
                    options =>
                    {
                        options.MinificationSettings.RemoveOptionalEndTags = false;
                        options.MinificationSettings.WhitespaceMinificationMode = WhitespaceMinificationMode.Safe;
                    });
            services.AddSingleton<IWmmLogger, WmmNullLogger>(); // Used by HTML minifier

            // Bundling, minification and Sass transpilation (https://github.com/ligershark/WebOptimizer)
            services.AddWebOptimizer(
                pipeline =>
                {
                    pipeline.MinifyJsFiles();
                    pipeline.CompileScssFiles()
                            .InlineImages(1);
                });
        }
    }
}