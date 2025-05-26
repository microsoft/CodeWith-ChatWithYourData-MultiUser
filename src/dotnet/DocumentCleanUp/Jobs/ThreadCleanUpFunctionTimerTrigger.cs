using System;
using Domain;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CleanUpJobs.Jobs
{
    public class ThreadCleanUpFunctionTimerTrigger
    {
        private readonly ILogger _logger;
        private IConfiguration _config;
        private IThreadRepository _threadRepository;
        private ThreadCleanup _threadCleanup;
        private int _threadCleanupDays =30;

        public ThreadCleanUpFunctionTimerTrigger(ILoggerFactory loggerFactory,
        IConfiguration config,
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
            var oldMessages = await _threadRepository.GetAllThreads(xDaysAgo);
            if (oldMessages.Count == 0)
            {
                _logger.LogInformation("No threads to clean up.");
                return;
            }

            var threads = oldMessages
                .Select(o => new { o.ThreadId, o.UserId })
                .Distinct()
                .ToDictionary(o => o.ThreadId, o => o.UserId);

            // await _threadCleanup.Cleanup(threadIds); // this hard deletes the thread,
            // going to update this to apply a soft delete
            foreach (var thread in threads)
            {
                // value is the userId
                // key is the threadId
                await _threadRepository.MarkThreadAsDeletedAsync(thread.Value, thread.Key);
            }


            // Log the next scheduled run time if available
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
