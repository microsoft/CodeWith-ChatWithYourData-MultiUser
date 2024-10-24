using System;
using System.Collections.Generic;
using Domain;
using Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Thread = Domain.Thread;

namespace DocumentCleanUp;

public class ThreadCleanUpFunctionFromCosmos
{
    private readonly ILogger _logger;
    private IConfiguration _config;
    private ThreadCleanup _threadCleanup;
    private static string _cosmosDbDatabaseName = string.Empty;
    private static string _cosmosDbContainerName = string.Empty;
    private static string _storageContainerName = string.Empty;

    public ThreadCleanUpFunctionFromCosmos(
        ILoggerFactory loggerFactory,
        IConfiguration config,
        ThreadCleanup threadCleanup
        )
    {
        _threadCleanup = threadCleanup;
        _logger = loggerFactory.CreateLogger<DocumentCleanUpFunction>();
        _config = config;
        _cosmosDbDatabaseName = _config["Cosmos:DatabaseName"] ?? throw new ArgumentNullException("Cosmos:DatabaseName");
        _cosmosDbContainerName = _config["Cosmos:Container"] ?? throw new ArgumentNullException("Cosmos:Container");
        _storageContainerName = _config["Storage:ContainerName"] ?? throw new ArgumentNullException("Storage:ContainerName");
    }

    [Function("DocumentCleanUp")]
    public async Task Run([CosmosDBTrigger(
        databaseName: "%CosmosDbDatabaseName%",
        containerName: "%CosmosDbContainerName%",
        Connection = "CosmosDbConnection",
        LeaseContainerName = "%CosmosDbLeaseContainer%",
        CreateLeaseContainerIfNotExists = false)] IReadOnlyList<Thread> threads)
    {
        _logger.LogInformation($"CosmosDbTrigger found {threads.Count} threads which are soft-deleted.");

        threads.Where(t => t.Deleted).ToList().ForEach(async t =>
        {
            _logger.LogInformation($"Thread {t.ThreadName} is marked for deletion. Deleting...");
            await _threadCleanup.Cleanup(t.Id);
        });
    }
}

