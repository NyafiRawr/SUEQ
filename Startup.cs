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

namespace SUEQ_API
{
    public class Startup
    {
        private readonly IConfiguration Configuration;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private int https_port;
        private bool ssl;

        public void ConfigureServices(IServiceCollection services)
        {
            // ��������� ���������� httpS
            https_port = Convert.ToInt32(Configuration["https_port"]);
            ssl = https_port != 0;
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

                            // ���������� � ��������� ��������������� ����
                            IssuerSigningKey = new SymmetricSecurityKey(
                                System.Text.Encoding.ASCII.GetBytes(Configuration["Token:Key"])),
                            // ��������� ����
                            ValidateIssuerSigningKey = true,
                        };
                    });
            // �������������� �����������
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            logger.LogInformation("SSL: {0}", ssl);

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
