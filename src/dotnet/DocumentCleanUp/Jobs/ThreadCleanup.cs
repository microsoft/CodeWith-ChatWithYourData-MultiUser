using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanUpJobs.Jobs
{
    public class ThreadCleanup
    {
        private readonly ILogger _logger;
        private IDocumentStore _documentStore;
        private IDocumentRegistry _documentRegistry;
        private IThreadRepository _threadRepository;

        public ThreadCleanup(ILoggerFactory loggerFactory, 
            IConfiguration config, 
            IDocumentStore documentStore, 
            IDocumentRegistry documentRegistry,
            IThreadRepository threadRepository)
        {
            _logger = loggerFactory.CreateLogger<ThreadCleanup>();
            _documentStore = documentStore;
            _documentRegistry = documentRegistry;
            _threadRepository = threadRepository;
        }

        public async Task Cleanup(List<string> threadIds)
        {
            foreach (string _threadId in threadIds)
            {
                await Cleanup(_threadId);
            }
        }

        public async Task<IActionResult> Cleanup(string threadId)
        {
            var thread = await _threadRepository.GetSoftDeletedThreadAsync(threadId);
            if (thread.Count == 0)
            {
                _logger.LogInformation($"Thread {threadId} not found.");
                return new NotFoundResult();
            }
            // get the userId of the thread as well to be used in the delete function
            var userId = thread[0].UserId;

            // get all the docs for thread
            var docs = await _documentRegistry.GetDocsPerThreadAsync(threadId);

            // set the documents in the documentRegistry to be soft deleted so that the DocumentCleanUpFunction can delete them
            if (await _documentRegistry.RemoveDocumentFromThreadAsync(docs))
            {
                //step 3: hard-delete chat history
               await _threadRepository.DeleteThreadAsync(userId, threadId);
            }

            return new OkResult();

        }

    }
}
