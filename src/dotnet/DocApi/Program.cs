
using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Storage;
using DocApi.Helpers;
using Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using System.Runtime.CompilerServices;

namespace DocApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            DefaultAzureCredentialOptions azureCredentialOptions =
            DefaultCredentialOptions.GetDefaultAzureCredentialOptions(builder.Environment);

            var azureCredential = new DefaultAzureCredential(azureCredentialOptions);
            // Add services to the container.
            builder.Services.AddAzureClients(clientBuilder =>
            {
                // Register clients for each service
                var blobConfig = builder.Configuration.GetSection("Storage");
                Uri serviceUri = new Uri(blobConfig["ServiceUri"]);
                clientBuilder.AddBlobServiceClient(serviceUri);

                clientBuilder.UseCredential(azureCredential);
            });

            var cosmosConfig = builder.Configuration.GetSection("Cosmos");
            if (cosmosConfig != null)
            {
                builder.Services.AddSingleton(sp =>
                {
                    string accountEndpoint = cosmosConfig["AccountEndpoint"];

                    // Create and configure CosmosClientOptions
                    var cosmosClientOptions = new CosmosClientOptions
                    {
                        ConnectionMode = ConnectionMode.Direct,
                        RequestTimeout = TimeSpan.FromSeconds(30)
                    };
                    return new CosmosClient(accountEndpoint, azureCredential, cosmosClientOptions);
                });
            }

            var searchConfig = builder.Configuration.GetSection("Search");
            if (searchConfig != null)
            {
                Uri serviceUri = new Uri(searchConfig["EndPoint"]);
                string indexName = searchConfig["IndexName"];

                builder.Services.AddSingleton(sp => new SearchClient(serviceUri, indexName, azureCredential));
                builder.Services.AddSingleton(sp => new SearchIndexClient(serviceUri, azureCredential));
                builder.Services.AddSingleton(sp => new SearchIndexerClient(serviceUri, azureCredential));
            }

            builder.Services.AddScoped<IDocumentRegistry, CosmosDocumentRegistry>();
            builder.Services.AddScoped<IDocumentStore, BlobDocumentStore>();
            builder.Services.AddScoped<ISearchService, AISearchService>();
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
