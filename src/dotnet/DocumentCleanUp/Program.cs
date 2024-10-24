using Azure.Identity;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents;
using Infrastructure;
using Infrastructure.Helpers;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using DocumentCleanUp;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        //read configuration
        var builder = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);

        var config = builder.Build();

        DefaultAzureCredentialOptions azureCredentialOptions =
            DefaultCredentialOptions.GetDefaultAzureCredentialOptions(config["ASPNETCORE_ENVIRONMENT"]);

        var azureCredential = new DefaultAzureCredential(azureCredentialOptions);
        // Add services to the container.
        services.AddAzureClients(clientBuilder =>
        {
            // Register clients for each service
            Uri serviceUri = new Uri(config["Storage:ServiceUri"]);
            clientBuilder.AddBlobServiceClient(serviceUri);
            clientBuilder.UseCredential(azureCredential);
        });


        services.AddSingleton(sp =>
        {
            string accountEndpoint = config["Cosmos:AccountEndpoint"];
            string cosmosDBDatabase = config["Cosmos:DatabaseName"];
            string cosmosDBContainer = config["Cosmos:Container"];

            // Create and configure CosmosClientOptions
            var cosmosClientOptions = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Direct,
                RequestTimeout = TimeSpan.FromSeconds(30)
            };

            var client = new CosmosClient(accountEndpoint, azureCredential, cosmosClientOptions);
            var database = client.GetDatabase(cosmosDBDatabase);
            return database.GetContainer(cosmosDBContainer);
        });

        Uri serviceUri = new Uri(config["Search:EndPoint"]);
        string indexName = config["Search:IndexName"];

        services.AddSingleton(sp => new SearchClient(serviceUri, indexName, azureCredential));
        services.AddSingleton(sp => new SearchIndexClient(serviceUri, azureCredential));
        services.AddSingleton(sp => new SearchIndexerClient(serviceUri, azureCredential));

        services.AddSingleton<ThreadCleanup>();
        services.AddScoped<IDocumentStore, BlobDocumentStore>();
        services.AddScoped<IDocumentRegistry, CosmosDocumentRegistry>();
        //services.AddSingleton<IConfiguration>(config);

        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();
