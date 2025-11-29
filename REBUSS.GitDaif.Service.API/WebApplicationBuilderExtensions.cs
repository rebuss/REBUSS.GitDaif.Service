using REBUSS.GitDaif.Service.API.Agents;
using REBUSS.GitDaif.Service.API.Properties;
using REBUSS.GitDaif.Service.API.Services;
using Serilog;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace REBUSS.GitDaif.Service.API
{
    public static class WebApplicationBuilderExtensions
    {
        public static WebApplicationBuilder SetupLogging(this WebApplicationBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();
            return builder;
        }

        public static WebApplicationBuilder SetupConfiguration(this WebApplicationBuilder builder)
        {
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables();
            builder.Services.Configure<AppSettings>(builder.Configuration);
            builder.Services.Configure<OpenAISettings>(builder.Configuration.GetSection("OpenAI"));
            
            return builder;
        }

        public static WebApplicationBuilder SetupServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddControllers();
            builder.Services.AddHostedService<DiffFileCleanerBackgroundService>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var logger = provider.GetRequiredService<ILogger<DiffFileCleanerBackgroundService>>();
                var appSettings = provider.GetRequiredService<IOptions<AppSettings>>().Value;
                return new DiffFileCleanerBackgroundService(appSettings.DiffFilesDirectory, logger);
            });
            var settings = builder.Configuration.GetSection("OpenAI").Get<OpenAISettings>();
            var kernel = GetKernel(settings);
            builder.Services.AddSingleton(kernel);
            builder.Services.AddScoped<IAIAgent, AzureOpenAI>();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddScoped<GitService>();
            return builder;
        }

        public static Kernel GetKernel(OpenAISettings settings)
        {
            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.Services.AddLogging().AddSerilog();
            kernelBuilder.AddAzureOpenAIChatCompletion(settings.Model, settings.Endpoint, settings.Key);
            
            return kernelBuilder.Build();
        }
    }
}
