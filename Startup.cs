using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
// ����������� ���� ������
using Microsoft.EntityFrameworkCore;
using SUEQ_API.Models;
// ��������� � ���������� �������� ��� appsettings
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
// ������������
using Microsoft.Extensions.Logging;
// JWT ��������������
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using Hanssens.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Net.Http;
using SUEQ_API.Services;

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
                Filename = "SUEQ-API.LOCALSTORAGE"
            })) { };
        }

        private int https_port;
        public static bool ssl;
        
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

        // ����������� �����������
        private static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];

            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // ��������� ���������� httpS
            https_port = Convert.ToInt32(Configuration["https_port"]);
            ssl = https_port != 0;
            if (ssl)
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
                    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect; // ������ ����������� �� ���������
                    options.HttpsPort = https_port; // �������� ������������ ������ ����������� � int32
                });
                // ��������� �������� �����������
                services.AddCertificateForwarding(options =>
                {
                    options.CertificateHeader = "X-SSL-CERT";
                    options.HeaderConverter = (headerValue) =>
                    {
                        X509Certificate2 clientCertificate = null;

                        if (!string.IsNullOrWhiteSpace(headerValue))
                        {
                            byte[] bytes = StringToByteArray(headerValue);
                            clientCertificate = new X509Certificate2(bytes);
                        }

                        return clientCertificate;
                    };
                });
                // ������������ ���� ���������� � ��� ��������
                var clientCertificate = new X509Certificate2(
                    Path.Combine(Configuration["Certificate:Path"]), Configuration["Certificate:Password"]);
                var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(clientCertificate);
                services.AddHttpClient<InterceptorCertificates>("CertificatedInterceptor", c =>
                {
                }).ConfigurePrimaryHttpMessageHandler(() => handler);
            }

            // ������������ ��� �������� ���� ������ � ������������
            services.AddDbContext<SUEQContext>(options => {
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
                        options.SaveToken = true;
                        options.RequireHttpsMetadata = ssl;
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
            logger.LogInformation("Status SSL: {0}", ssl);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // �������� ��������� Strict-Transport-Security, ����� �������� ��������� � ������������� http
                // ��� ����������� ����������� ������
                if (ssl) app.UseHsts();
            }

            // ��������������� �� httpS
            if (ssl) app.UseHttpsRedirection();

            app.UseRouting();

            if (ssl) app.UseCertificateForwarding();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // ���� ���������� ����������� � ������� ���������� ��
                endpoints.MapControllers();
                // ����� ��� �������� � �������� ����
                endpoints.MapGet("/", async context =>
                {
                    logger.LogInformation("Processing request {0}", context.Request.Path);
                    var sb = new System.Text.StringBuilder()
                        .Append("<center><h1>��������� �������� � ��������� �������!</center></h1>");
                    context.Response.ContentType = "text/html;charset=utf-8";
                    await context.Response.WriteAsync(sb.ToString());
                });
            });
        }
    }
}
