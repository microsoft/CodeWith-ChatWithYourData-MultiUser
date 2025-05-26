using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using CleanUpJobs.Jobs;
using Infrastructure;
using Infrastructure.Helpers;
using Infrastructure.Implementations.AISearch;
using Infrastructure.Implementations.Blob;
using Infrastructure.Implementations.Cosmos;
using Infrastructure.Implementations.SPE;
using Infrastructure.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()

   .ConfigureServices((hostContext, services) =>
   {

       DefaultAzureCredentialOptions azureCredentialOptions =
             DefaultCredentialOptions.GetDefaultAzureCredentialOptions(hostContext.Configuration["ASPNETCORE_ENVIRONMENT"]);
       var azureCredential = new DefaultAzureCredential(azureCredentialOptions);

       // ---------- SharePoint Embedded Implementation ----------------
       services.AddScoped<MSGraphService>(); // this is needed to work with SharePoint Embedded
       services.AddScoped<IDocumentStore, SPEDocumentStore>(); // this makes use of SharePoint Embedded

       // -----------------------------------------------------

       // ---------- Azure Blob Implementation ----------------
       //services.AddAzureClients(clientBuilder =>
       //{
       //    // Register clients for each service
       //    Uri serviceUri = new Uri(hostContext.Configuration["StorageServiceUri"]);
       //    clientBuilder.AddBlobServiceClient(serviceUri);
       //    clientBuilder.UseCredential(azureCredential);
       //});
       //services.AddSingleton<IDocumentStore, BlobDocumentStore>();
       // -----------------------------------------------------

       Uri serviceUri = new Uri(hostContext.Configuration["SearchEndPoint"]);
       string indexName = hostContext.Configuration["SearchIndexName"];

       services.AddSingleton(sp => new SearchClient(serviceUri, indexName, azureCredential));
       services.AddSingleton(sp => new SearchIndexClient(serviceUri, azureCredential));
       services.AddSingleton(sp => new SearchIndexerClient(serviceUri, azureCredential));

       // Setting up the Cosmosdb client
       services.AddSingleton(sp =>
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


       services.AddSingleton<IDocumentRegistry, CosmosDocumentRegistry>();
       services.AddSingleton<ISearchService, AISearchService>();       
       services.AddSingleton<IThreadRepository, CosmosThreadRepository>();

       services.AddSingleton<ThreadCleanup>();
       services.AddSingleton<DocumentCleanUpFunction>();
       services.AddSingleton<ThreadCleanUpFunctionFromCosmos>();
       services.AddSingleton<ThreadCleanUpFunctionTimerTrigger>();

   })
   .Build();

host.Run();
