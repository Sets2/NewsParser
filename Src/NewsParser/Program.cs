using Microsoft.OpenApi.Models;
using NewsParser.Parser;
using System.Reflection;
using DataAccess;
using DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Otus.Teaching.Pcf.Administration.Integration;

namespace NewsParser
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            RegisterServices(builder);
            var app = builder.Build();
            await Configure(app);

            app.Run();
        }

        static void RegisterServices(WebApplicationBuilder builder)
        {
            IServiceCollection services = builder.Services;
            services.Configure<ParserSettings>(builder.Configuration.GetSection("ParserSettings"));

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "NewsParser.WebApi", Version = "v1" });
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });

            services.AddDbContext<DataContext>(option =>
            {
                option.UseNpgsql(builder.Configuration.GetConnectionString("SqlDb"));
                option.EnableSensitiveDataLogging();
                //option.UseLazyLoadingProxies();   // Возможно подключение "ленивой" загрузки подчиненных сущностей

            });
            services.AddScoped<IDbInitializer, DbInitializer>();

            services.AddMvc(options =>
            {
                options.SuppressAsyncSuffixInActionNames = false;
            });
            services.AddScoped<IDbInitializer, DbInitializer>();
            services.AddTransient<IParser, XmlParser>();
            services.AddSingleton<IGroupChannels, GroupChannels>();
            services.AddHostedService<ParserHostedService>();
        }

        static async Task Configure(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));

            app.UseHttpsRedirection();
            app.UseRouting();
            app.MapControllers();

            using var scope = app.Services.CreateScope();
            {
                var services = scope.ServiceProvider;
                try
                {
                    var dbInit = services.GetRequiredService<IDbInitializer>();
                    await dbInit.InitializeDb();
                    await Task.Delay(0);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occured during open and initialization");
                }
            }
        }
    }
}