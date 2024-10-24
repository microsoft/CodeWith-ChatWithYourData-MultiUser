using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using Domain;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Thread = Domain.Thread;
using Container = Microsoft.Azure.Cosmos.Container;
using Microsoft.Azure.Cosmos.Linq;

namespace Infrastructure
{

    public class CosmosThreadRepository : IThreadRepository
    {
        private Container _container;

        public CosmosThreadRepository(Container cosmosDbContainer)
        {
            _container = cosmosDbContainer;
        }

        public List<ThreadMessage> GetAllThreads(DateTime expirationDate)
        {
            IQueryable<ThreadMessage> threads = _container
                .GetItemLinqQueryable<ThreadMessage>(allowSynchronousQueryExecution: true)
                .Where(o => o.Created <= expirationDate);

            return threads.ToList();
        }

        public List<string> GetAllThreadIds(DateTime expirationDate)
        {
            IQueryable<string> threadIds = _container
                .GetItemLinqQueryable<ThreadMessage>(allowSynchronousQueryExecution: true)
                .Where(o => o.Created <= expirationDate)
                .Select(o => o.ThreadId)
                .Distinct();

            return threadIds.ToList();
        }

        public async Task<List<Thread>> GetThreadsAsync(string userId)
        {
            var threadsQuery = _container
                .GetItemLinqQueryable<Thread>(allowSynchronousQueryExecution: true)
                .Where(t => t.UserId == userId && t.Type == "CHAT_THREAD" && !t.Deleted)
                .OrderByDescending(t => t.ThreadName);

            var iterator = threadsQuery.ToFeedIterator();

            var threads = new List<Thread>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                threads.AddRange(response);
            }

            return threads;
        }

        public async Task<bool> MarkThreadAsDeletedAsync(string userId, string threadId)
        {
            var fieldsToUpdate = new Dictionary<string, object>
            {
                { "deleted", true },
            };

            try
            {
                return await UpdateThreadFieldsAsync(threadId, userId, fieldsToUpdate);
            }
            catch (CosmosException cosmosEx)
            {
                throw new Exception($"Failed to mark thread as deleted: {cosmosEx.Message}", cosmosEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while marking thread as deleted: {ex.Message}", ex);
            }
        }

        internal async Task<bool> UpdateThreadFieldsAsync(string threadId, string userId, Dictionary<string, object> fieldsToUpdate)
        {
            var patchOperations = new List<PatchOperation>();

            foreach (var field in fieldsToUpdate)
            {
                patchOperations.Add(PatchOperation.Set($"/{field.Key}", field.Value));
            }

            try
            {
                var response = await _container.PatchItemAsync<Thread>(threadId, new PartitionKey(userId), patchOperations);
                return response.StatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (CosmosException ex)
            {
                // Handle exception
                throw new Exception($"Failed to update thread: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteThreadAsync(string userId, string threadId)
        {
            Domain.Thread thread = await _container.ReadItemAsync<Domain.Thread>(threadId, new PartitionKey(userId));
            if (thread == null)
            {
                return false;
            }

            var messages = await this.GetMessagesAsync(userId, threadId);

            foreach (ThreadMessage message in messages)
            {
                await _container.DeleteItemAsync<ThreadMessage>(message.Id, new PartitionKey(userId));
            }

            thread.Deleted = true;

            var response = await _container.ReplaceItemAsync<Domain.Thread>(thread, threadId, new PartitionKey(userId));

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return false;
            }
            return true;

        }

        public async Task<Domain.Thread> CreateThreadAsync(string userId)
        {
            var newThread = new Domain.Thread
            {
                Id = Guid.NewGuid().ToString(),
                Type = "CHAT_THREAD",
                UserId = userId,
                ThreadName = DateTime.Now.ToString("dd MMM yyyy, HH:mm")
            };

            var response = await _container.CreateItemAsync<Domain.Thread>(newThread, new PartitionKey(userId));
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                throw new Exception("Failed to create a new thread.");
            }
            return response;

        }

        public async Task<List<ThreadMessage>> GetMessagesAsync(string userId, string threadId)
        {
            var messagesQuery = _container
                .GetItemLinqQueryable<ThreadMessage>(allowSynchronousQueryExecution: true)
                .Where(m => m.ThreadId == threadId)
                .OrderBy(m => m.Created);

            var iterator = messagesQuery.ToFeedIterator();

            var messages = new List<ThreadMessage>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                messages.AddRange(response);
            }

            return messages;
        }

        public async Task<bool> PostMessageAsync(string userId, string threadId, string message, string role)
        {
            string messageId = Guid.NewGuid().ToString();
            DateTime now = DateTime.Now;

            ThreadMessage newMessage = new()
            {

                Id = messageId,
                Type = "CHAT_MESSAGE",
                ThreadId = threadId,
                UserId = userId,
                Role = role,
                Content = message,
                Created = DateTime.Now
            };

            var response = await _container.CreateItemAsync<ThreadMessage>(newMessage, new PartitionKey(userId));
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                throw new Exception("Failed to create a new thread.");
            }
            return true;
        }

    }
}