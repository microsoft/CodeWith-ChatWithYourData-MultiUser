using System;
using System.Collections.Generic;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Thread = Domain.Cosmos.Thread;

namespace CleanUpJobs.Jobs;

public class ThreadCleanUpFunctionFromCosmos
{
    private readonly ILogger _logger;
    private ThreadCleanup _threadCleanup;

    public ThreadCleanUpFunctionFromCosmos(
        ILoggerFactory loggerFactory,
        ThreadCleanup threadCleanup
        )
    {
        _threadCleanup = threadCleanup;
        _logger = loggerFactory.CreateLogger<ThreadCleanUpFunctionFromCosmos>();
    }

    [Function("ThreadCleanUpFromCosmos")]
    public async Task<IActionResult> Run([CosmosDBTrigger(
        databaseName: "%CosmosDBDatabase%",
        containerName: "%CosmosDbThreadContainer%",
        Connection = "CosmosDbConnection",
        LeaseContainerName ="%CosmosDbThreadLease%",
        FeedPollDelay = 5000, // this to ensure that when deleting messages from a thread the trigger is not triggered again
        CreateLeaseContainerIfNotExists = false)] IReadOnlyList<Thread> threads)
    {
        if (threads == null || threads.Count == 0)
            return new OkResult();

        _logger.LogInformation($"CosmosDbTrigger found {threads.Count} threads which are updated");

        for (int i = 0; i < threads.Count; i++)
        {
            var thread = threads[i];
            if (thread.Type != "CHAT_THREAD")
            {
                _logger.LogInformation($"Thread {thread.ThreadName} is not a thread. Skipping...ffs!");
                continue;
            }
            if (thread.Deleted)
            {
                _logger.LogInformation($"Thread {thread.ThreadName} is marked for deletion. Deleting...");
                await _threadCleanup.Cleanup(thread.Id);

            }
        }
   
        return new OkResult();
    }
}

