using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NETCore.MailKit.Extensions;
using NETCore.MailKit.Infrastructure.Internal;
using Spice.Models;
using Spice.Services;
using Spice.Utilities;
using Spice.Validators;
using Stripe;
using Stripe.BillingPortal;

namespace Spice {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
            StripeConfiguration.ApiKey =
                "sk_test_51I9C85F9AODfnpB1KCouR1uyomvh4oWzVjOiWyOfCIWXn5ckSoTNyIUYuocuhG4J2hOJWzvr4gN4bLeqwThDfF9f00rIb64SYv";
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
            services.AddIdentity<IdentityUser, IdentityRole>(options => {
                    options.SignIn.RequireConfirmedEmail = true;
                    options.SignIn.RequireConfirmedPhoneNumber = false;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
            services.AddSingleton<IEmailSender, EmailSender>();
            services.AddControllersWithViews().AddRazorRuntimeCompilation()
                .AddFluentValidation(f => f.RegisterValidatorsFromAssemblyContaining<CategoryValidator>());
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddAutoMapper(typeof(Startup));
            services.Configure<StripeSetting>(Configuration.GetSection("Stripe"));
            var mailkit = Configuration.GetSection("Email").Get<MailKitOptions>();
            services.AddMailKit(config =>
                config.UseMailKit(mailkit));
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.LogoutPath = "/Identity/Account/Logout";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });

            services.AddSession(option => {
                option.Cookie.IsEssential = true;
                option.IdleTimeout = TimeSpan.FromMinutes(10);
                option.Cookie.HttpOnly = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();
            
            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
