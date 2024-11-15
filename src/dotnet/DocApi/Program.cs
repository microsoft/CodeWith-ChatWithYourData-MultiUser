using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Storage;
using Infrastructure.Helpers;
using Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using DocApi.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Http.Features;
using WebApi.Extensions;

namespace DocApi
{
    public class Program
    {
    
    public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // setting the managed identity for the app (locally this reverts back to the VS/VSCode/AZCli credentials)
            DefaultAzureCredentialOptions azureCredentialOptions = DefaultCredentialOptions.GetDefaultAzureCredentialOptions(builder.Environment.EnvironmentName);

            var azureCredential = new DefaultAzureCredential(azureCredentialOptions);
            
            // Setting up the Azure Blob Storage client
            builder.Services.AddAzureClients(clientBuilder =>
            {
                var blobConfig = builder.Configuration.GetSection("Storage");
                var serviceUri = new Uri(blobConfig["ServiceUri"]);
                clientBuilder.AddBlobServiceClient(serviceUri).WithCredential(azureCredential);
            });

            // Setting up the Cosmosdb client
            builder.Services.AddSingleton(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var accountEndpoint = configuration["Cosmos:AccountEndpoint"];
                var cosmosClientOptions = new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = TimeSpan.FromSeconds(30)
                };
                return new CosmosClient(accountEndpoint, azureCredential, cosmosClientOptions);
            });

            // Setting up the Search client
            builder.Services.AddSingleton(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var serviceUri = new Uri(configuration["Search:EndPoint"]);
                return new SearchIndexClient(serviceUri, azureCredential);
            });

            // Setting up the Semantic Kernel and AI Search with Vectors and Embeddings
            builder.AddSemanticKernel(azureCredential, null);
            builder.AddAzureAISearch(azureCredential, null);

            // Setting up the interfaces and implentations to be used in the controllers
            builder.Services.AddSingleton<IDocumentRegistry, CosmosDocumentRegistry>();
            builder.Services.AddSingleton<IDocumentStore, BlobDocumentStore>();
            builder.Services.AddSingleton<ISearchService, AISearchService>();
            builder.Services.AddSingleton<IThreadRepository, CosmosThreadRepository>();

            // file upload limit -- need to work on this, still limited to 30MB
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB
            });

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Auth
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

            var app = builder.Build();
            app.UseSwagger();
            app.UseSwaggerUI();

            // CORS.
            app.UseCors(builder => builder
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed((host) => true)
                .AllowCredentials()
            );

            app.UseAuthentication();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}