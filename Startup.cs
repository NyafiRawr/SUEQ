using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
// ���������� �������� ��� appsettings
using Microsoft.Extensions.Configuration;
// ����������� ���� ������
using Microsoft.EntityFrameworkCore;
using SUEQ_API.Models;
// StringBuilder
using System;

namespace SUEQ_API
{
    public class Startup
    {
        private readonly IConfiguration Configuration;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // ��������� ������������� ��������� STS
            services.AddHsts(options =>
            {
                options.Preload = true; // ����������� �������
                options.IncludeSubDomains = true; // �������� ���������
                options.MaxAge = TimeSpan.FromDays(30); // ����� ���� ����� 30 ����, ���� ����� ��������� ���������� ���� 0
            });
            // ��������� ��������������� �� httpS
            services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect; // ������ ����������� �� ���������
                options.HttpsPort = 44344; // ���� �� ��������� 443, ��� ������� ����� ���������� 44344
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
            // �������������� �����������
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // �������� ��������� Strict-Transport-Security, ����� �������� ��������� � ������������� http
                // ��� ����������� ����������� ������
                app.UseHsts();
            }
            // ��������������� �� httpS
            app.UseHttpsRedirection();

            app.UseRouting();
            // ��������� �����������
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // ���� ���������� ����������� � ������� ���������� ��
                endpoints.MapControllers();
                // ����� ��� �������� � �������� ����
                endpoints.MapGet("/", async context =>
                {
                    var sb = new System.Text.StringBuilder()
                        .Append("<center><h1>��������� �������� � ��������� �������!</center></h1>");
                    context.Response.ContentType = "text/html;charset=utf-8";
                    await context.Response.WriteAsync(sb.ToString());
                });
            });
        }
    }
}
