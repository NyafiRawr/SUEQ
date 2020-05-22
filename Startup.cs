// ��������������
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
// ����������� ���� ������
using Microsoft.EntityFrameworkCore;
using Hanssens.Net;
// ��������� � ���������� �������� ��� appsettings
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
// ������������
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SUEQ_API.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace SUEQ_API
{
    public class Startup
    {
        public static IConfiguration Configuration;
        public static LocalStorage Storage;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            using (Storage = new LocalStorage(new LocalStorageConfiguration()
            {
                // EnableEncryption = false,
                // EncryptionSalt = "LocalStorage",
                AutoLoad = true,
                AutoSave = false,
                Filename = "Properties/SUEQ-API.LOCALSTORAGE"
            })) { };
        }

        private bool CustomLifetimeValidator(DateTime? notBefore, DateTime? expires, SecurityToken token, TokenValidationParameters @params)
        {
            if (expires != null)
            {
                return expires < DateTime.Now;
            }

            return false;
        }

        private static Task AdditionalValidation(TokenValidatedContext context)
        {
            var userId = context.Principal.FindFirst("UserId").Value;
            if (userId == null)
            {
                context.Fail("User not found");
                return Task.CompletedTask;
            }

            // ������������� ������ ������ ���������� �������������
            string lastAccessToken;
            try
            {
                lastAccessToken = Storage.Get<string>(userId);
            }
            catch
            {
                context.Fail("User not authenticated");
                return Task.CompletedTask;
            }
            // ������������� ������ ������ �� ���������� ������ �������
            var token = new JwtSecurityTokenHandler().WriteToken(context.SecurityToken);
            lastAccessToken = lastAccessToken.Remove(lastAccessToken.LastIndexOf('.') + 1);
            if (lastAccessToken != token)
            {
                context.Fail("Token Expired");
            }
            return Task.CompletedTask;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            if (Program.Ssl)
            {
                // ��������� ������������� ��������� STS
                services.AddHsts(options =>
                {
                    options.Preload = true; // ����������� �������
                    options.IncludeSubDomains = true; // �������� ���������
                    options.MaxAge = TimeSpan.FromDays(30); // ����� ���� ����� 30 ����
                });
                // ��������� �������������
                services.AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
                    options.HttpsPort = Program.Https_port;
                });
            }

            // ������������ ��� �������� ���� ������ � ������������
            services.AddDbContext<SUEQContext>(options =>
            {
                options.UseMySql(
                    $"server={Configuration["DataBase:Host"]};" +
                    $"database={Configuration["DataBase:Name"]};" +
                    $"port={Configuration["DataBase:Port"]};" +
                    $"uid={Configuration["DataBase:User"]};" +
                    $"password={Configuration["DataBase:Password"]};"
                );
            });
            // �������� JWT
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.SaveToken = false;
                        options.RequireHttpsMetadata = Program.Ssl;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            // ��������� ��������
                            ValidateIssuer = true,
                            // ��������� ��� ��
                            ValidIssuer = Configuration["Token:Issuer"],

                            // ��������� �����������
                            ValidateAudience = true,
                            // ��������� ��� ��
                            ValidAudience = Configuration["Token:Audience"],
                            // ��������� ���� �����
                            ValidateLifetime = true,
                            LifetimeValidator = CustomLifetimeValidator,
                            // ��������� ����
                            ValidateIssuerSigningKey = true,
                            // ���������� � ��������� ��������������� ����
                            IssuerSigningKey = new SymmetricSecurityKey(
                                System.Text.Encoding.ASCII.GetBytes(Configuration["Token:Key"])),
                            // ���������� �������� ����� (����� +5 ����� � expires)
                            ClockSkew = TimeSpan.Zero
                        };
                        options.Events = new JwtBearerEvents
                        {
                            OnTokenValidated = AdditionalValidation
                        };
                    });
            // �������������� �����������
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            logger.LogInformation("Status SSL: {0} | Port: {1}", Program.Ssl, Program.Https_port);

            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            // ��������������� �� httpS
            if (Program.Ssl)
            {
                app.UseHsts();
                // �������������� �� httpS, ����� �������� ��������� � ������������� http
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // ���� ���������� ����������� � ������� ���������� ��
                endpoints.MapControllers();
                // ����� ��� �������� � �������� ����
                endpoints.MapGet("/", async context =>
                {
                    logger.LogInformation("Someone went to the root {0}", context.Request.Path);
                    var sb = new System.Text.StringBuilder()
                        .Append("<center><h1>��������� �������� � ��������� �������!</center></h1>");
                    context.Response.ContentType = "text/html;charset=utf-8";
                    await context.Response.WriteAsync(sb.ToString());
                });
            });
        }
    }
}
