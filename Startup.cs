using System;
using AutoMapper;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NETCore.MailKit.Extensions;
using NETCore.MailKit.Infrastructure.Internal;
using Spice.Services;
using Spice.Utilities;
using Spice.Validators;
using Stripe;
using Twilio;

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
            services.AddCors(option => option.AddPolicy(name: "_myAllowSpecificOrigins",
                              builder => {
                                  builder.WithOrigins("https://spice20210316022610.azurewebsites.net/");
                              })
            );
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
            services.AddIdentity<IdentityUser, IdentityRole>(options => {
                    options.SignIn.RequireConfirmedEmail = true;
                    options.SignIn.RequireConfirmedPhoneNumber = false;
                options.Password.RequireNonAlphanumeric = false;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders().AddDefaultUI();
            services.AddScoped<IDbInitializer, DbInitializer>();
            services.AddAuthentication()
                .AddFacebook(options => {
                    options.AppId = "138616134833745";
                    options.AppSecret = "6298932f263bf10c6357aaceb623c9f8";
                })
                .AddGoogle(options => {
                    options.ClientId = "831539473196-f8tan6jcm5533n20ismjnkdrsfv219bn.apps.googleusercontent.com";
                    options.ClientSecret = "YhforD5_P2SCEvZFrdT5DNwF";
                });
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
            
            services.Configure<SecurityStampValidatorOptions>(options =>
            {
                options.ValidationInterval = TimeSpan.FromSeconds(10);
                
            });
            
            var accountSid = Configuration["Twilio:AccountSID"];
            var authToken = Configuration["Twilio:AuthToken"];
            TwilioClient.Init(accountSid, authToken);
            services.Configure<TwilioVerifySettings>(Configuration.GetSection("Twilio"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,IDbInitializer dbInitializer) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            dbInitializer.Initialize().GetAwaiter().GetResult();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            
            app.UseRouting();
            app.UseCors("_myAllowSpecificOrigins");
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
