using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentCleanUp
{
    public class ThreadCleanup
    {
        private readonly ILogger _logger;
        private IConfiguration _config;
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
            _config = config;
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

        public async Task Cleanup(string threadId)
        {
            var thread = await _threadRepository.GetThreadsAsync(threadId);
            // get the userId of the thread as well to be used in the delete function
            var userId = thread[0].UserId;

            // step 2: soft delete alle documenten die bij deze thread horen (dit triggered DocCleanUp)
            // get all the docs for thread
            var docs = _documentRegistry.GetDocsPerThreadAsync(threadId).GetAwaiter().GetResult();

            // set the documents in the documentRegistry to be soft deleted so that the DocumentCleanUpFunction can delete them
            if (_documentRegistry.RemoveDocumentFromThreadAsync(docs).GetAwaiter().GetResult())
            {
                //step 3: hard-delete chat history
                _threadRepository.DeleteThreadAsync(userId, threadId).GetAwaiter().GetResult();
            }
        }

    }
}
