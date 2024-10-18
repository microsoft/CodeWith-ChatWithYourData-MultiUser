//read the appsettings.json file
using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using Domain;
using Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.IO.Enumeration;

var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

var configuration = builder.Build();

var entry = new DocsPerThread
{
    Deleted = false,
    DocumentName = "Brochure Elektrisch Rijden nieuw.pdf",
    Id = "f9ec7b4e-8ba7-4e12-a593-7c5fbbc52cc0",
    ThreadId = "1234",
    UserId = "1234"
};


var searchClient = new SearchClient(new Uri(configuration["Search:Endpoint"]), configuration["Search:IndexName"], new AzureKeyCredential(configuration["Search:ApiKey"]));
var searchIndexClient = new SearchIndexClient(new Uri(configuration["Search:Endpoint"]), new AzureKeyCredential(configuration["Search:ApiKey"]));
var searchIndexerClient = new SearchIndexerClient(new Uri(configuration["Search:Endpoint"]), new AzureKeyCredential(configuration["Search:ApiKey"]));
//await searchIndexerClient.RunIndexerAsync(configuration["Search:IndexerName"]);

string threadId = "1234";
SearchOptions options = new SearchOptions();
// options.Filter = $"thread_id = '{threadId}'";
options.Select.Add("chunk_id");
options.Select.Add("title");
options.Select.Add("file_name");
options.Select.Add("document_id");

SearchResults<SearchDocument> response = await searchClient.SearchAsync<SearchDocument>(null, options);
int count = 0;
await foreach (SearchResult<SearchDocument> result in response.GetResultsAsync())
{
    if (entry.Id == result.Document["document_id"])
    {
        entry.AvailableInSearchIndex = true;
    }
    //Console.WriteLine($"Title: {result.Document["title"]}");
    //Console.WriteLine($"Score: {result.Score}\n");
    //Console.WriteLine($"Content: {result.Document["content"]}");
    //Console.WriteLine($"Category: {result.Document["category"]}\n");
}

Console.Read();


//Console.Read();

//search_datasource = AzureSearchDatasource(self.env_helper)
//            search_datasource.create_or_update_datasource()
//            search_index = AzureSearchIndex(self.env_helper, self.llm_helper)
//            search_index.create_or_update_index()
//            search_skillset = AzureSearchSkillset(
//                self.env_helper, config.integrated_vectorization_config
//            )
//            search_skillset_result = search_skillset.create_skillset()
//            search_indexer = AzureSearchIndexer(self.env_helper)
//            indexer_result = search_indexer.create_or_update_indexer(
//                self.env_helper.AZURE_SEARCH_INDEXER_NAME,
//                skillset_name = search_skillset_result.name,
//            )

////create a blob client
//var serviceUri = $"https://{configuration["StorageAccountName"]}.blob.core.windows.net";
//var blobServiceClient = new BlobServiceClient(new Uri(serviceUri), new AzureCliCredential());
//IDocumentStore store = new BlobDocumentStore(blobServiceClient);

////create a cosmos client
//var cosmosClient = new CosmosClient(configuration["CosmosDbAccountEndpoint"], new AzureCliCredential());
//var database = cosmosClient.GetDatabase(configuration["CosmosDBDatabase"]);
//var container = database.GetContainer(configuration["CosmosDBContainer"]);



////upload the files in folder Docs

//var threadId = Guid.NewGuid().ToString();

//var docsFolder = Directory.GetCurrentDirectory() + "/Docs";
//var docs = new DirectoryInfo(docsFolder).GetFiles();
//foreach (var doc in docs)
//{
//    var docId = await store.AddDocumentAsync(doc.FullName, threadId, configuration["StorageContainerName"]);
//    Console.WriteLine($"Uploaded {doc.Name}");


//       var entry = new DocsPerThread
//    {
//        Deleted = false,
//        DocumentName = doc.Name,
//        Id = docId,
//        ThreadId = threadId,
//        UserId = "test@microsoft.com"

//    };

//    //upload item to cosmosDb
//    await container.UpsertItemAsync(entry, new PartitionKey(entry.id));







//}



