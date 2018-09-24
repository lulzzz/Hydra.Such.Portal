﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Hydra.Such.Portal.Configurations;
using Hydra.Such.Data.NAV;
using Hydra.Such.Portal.Extensions;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Hydra.Such.Portal.Filters;

namespace Hydra.Such.Portal
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options => {
                options.Filters.Add(new NavigationFilter());
            });
            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
            });
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddOpenIdConnect(option =>
            {
                option.ClientId = Configuration["AzureAD:ClientId"];
                option.Authority = String.Format(Configuration["AzureAd:AadInstance"], Configuration["AzureAd:Tenant"]);
                option.SignedOutRedirectUri = Configuration["AzureAd:PostLogoutRedirectUri"];
                option.Events = new OpenIdConnectEvents
                {
                    OnRemoteFailure = OnAuthenticationFailed,
                };
            })
            .AddCookie();


            // ABARROS -> ADD NAV CONFIGURATIONS TO THE SERVICE
            var NAVConfigurations = Configuration.GetSection("NAVConfigurations");
            services.Configure<NAVConfigurations>(NAVConfigurations);

            // ABARROS -> ADD NAV WS CONFIGURATIONS TO THE SERVICE
            var NAVWSConfigurations = Configuration.GetSection("NAVWSConfigurations");
            services.Configure<NAVWSConfigurations>(NAVWSConfigurations);

            // ABARROS -> ADD NAV CONFIGURATIONS TO THE SERVICE
            var GeneralConfigurations = Configuration.GetSection("GeneralConfigurations");
            services.Configure<GeneralConfigurations>(GeneralConfigurations);


            // ABARROS -> Activate Session Variables
            services.AddSession(s => s.IdleTimeout = TimeSpan.FromMinutes(30));

            Data.Database.SuchDBContext.ConnectionString = Configuration.GetConnectionString("DefaultConnection");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseSession();

            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseDeveloperExceptionPage();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "area",
                    template: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            //app.UseMvc(routes =>
            //{
            //    routes.MapRoute(
            //      name: "areas",
            //      template: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
            //    );
            //});
        }

        // Handle sign-in errors differently than generic errors.
        private Task OnAuthenticationFailed(RemoteFailureContext context)
        {
            context.HandleResponse();
            context.Response.Redirect("/Error/Login?message=" + context.Failure.Message);
            return Task.FromResult(0);
        }

    }
}
