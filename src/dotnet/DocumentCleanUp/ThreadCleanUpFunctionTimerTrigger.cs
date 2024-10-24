using System;
using Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocumentCleanUp
{
    public class ThreadCleanUpFunctionTimerTrigger
    {
        private readonly ILogger _logger;
        private IConfiguration _config;
        private IThreadRepository _threadRepository;
        private ThreadCleanup _threadCleanup;
        private static string _cosmosDbDatabaseName = string.Empty;
        private static string _cosmosDbContainerName = string.Empty;
        private static string _storageContainerName = string.Empty;
        private int _threadCleanupDays = 7;

        public ThreadCleanUpFunctionTimerTrigger(ILoggerFactory loggerFactory,
        IConfiguration config,
        IDocumentStore documentStore,
        IDocumentRegistry documentRegistry,
        ISearchService searchService,
        IThreadRepository threadRepository,
        ThreadCleanup threadCleanup)
        {
            _config = config;
            _logger = loggerFactory.CreateLogger<ThreadCleanUpFunctionTimerTrigger>();
            _threadRepository = threadRepository;
            _threadCleanup = threadCleanup;
        }

        [Function("ThreadCleanUp")]
        public async Task Run([TimerTrigger("%ThreadCleanupCron%")] TimerInfo myTimer)
        {
            _logger.LogInformation($"Thread Cleanup function executed at: {DateTime.Now}");

            // Calculate the date for threads to be cleaned up
            DateTime xDaysAgo = DateTime.Now.AddDays(-_threadCleanupDays);

            // Retrieve all thread IDs that need to be cleaned up
            var threadIds = _threadRepository.GetAllThreadIds(xDaysAgo);

            // Perform the cleanup asynchronously
            await _threadCleanup.Cleanup(threadIds);

            // Log the next scheduled run time if available
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
