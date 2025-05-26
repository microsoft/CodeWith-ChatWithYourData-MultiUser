using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
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
using Thread = Domain.Cosmos.Thread;
using Container = Microsoft.Azure.Cosmos.Container;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using Domain.Cosmos;
using Infrastructure.Interfaces;

namespace Infrastructure.Implementations.Cosmos
{

    public class CosmosThreadRepository : IThreadRepository
    {
        private readonly CosmosClient _client;
        private readonly IConfiguration _configuration;
        private Container _container;

        public CosmosThreadRepository(CosmosClient client, IConfiguration configuration)
        {
            _client = client;
            _configuration = configuration;

            string databaseName = _configuration.GetValue<string>("Cosmos:DatabaseName") ?? "chats";
            string containerName = _configuration.GetValue<string>("Cosmos:ThreadHistoryContainerName") ?? "threadhistory";
            _container = _client.GetContainer(databaseName, containerName);
        }
        public async Task<List<Thread>> GetAllThreads()
        {
            List<Thread> threads = new List<Thread>();
            IQueryable<Thread> threadsQuery = _container
                .GetItemLinqQueryable<Thread>(allowSynchronousQueryExecution: true);

            var iterator = threadsQuery.ToFeedIterator();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                threads.AddRange(response);
            }

            return threads;
        }

        public async Task<List<ThreadMessage>> GetAllThreads(DateTime expirationDate)
        {
            List<ThreadMessage> threads = new List<ThreadMessage>();
            IQueryable<ThreadMessage> threadsQuery = _container
                .GetItemLinqQueryable<ThreadMessage>(allowSynchronousQueryExecution: true)
                .Where(o => o.Created <= expirationDate);

            var iterator = threadsQuery.ToFeedIterator();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                threads.AddRange(response);
            }

            return threads;
        }

        public async Task<List<string>> GetAllThreadIds(DateTime expirationDate)
        {
            List<string> threadIds = new List<string>();

            IQueryable<string> threadIdsQuery = _container
                .GetItemLinqQueryable<ThreadMessage>(allowSynchronousQueryExecution: true)
                .Where(o => o.Created <= expirationDate)
                .Select(o => o.ThreadId)
                .Distinct();

            var iterator = threadIdsQuery.ToFeedIterator();

            var threads = new List<Thread>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                threadIds.AddRange(response);
            }

            return threadIds;
        }

        public async Task<List<Thread>> GetSoftDeletedThreadAsync(string threadId)
        {
            var threadsQuery = _container
                .GetItemLinqQueryable<Thread>(allowSynchronousQueryExecution: true)
                .Where(t => t.Id == threadId && t.Type == "CHAT_THREAD" && t.Deleted)
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

        public async Task<bool> DeleteMessages(string userId, string threadId, CancellationToken cancellationToken)
        {
            bool isDeleted = false;
            var messages = await GetMessagesAsync(userId, threadId, cancellationToken);
            foreach(ThreadMessage message in messages)
            {
                try
                {
                    await _container.DeleteItemAsync<ThreadMessage>(message.Id, new PartitionKey(userId), cancellationToken: cancellationToken);
                }
                catch (CosmosException ex)
                {
                    throw new Exception($"Failed to delete message: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"An error occurred while deleting message: {ex.Message}", ex);
                }
                finally
                {
                    isDeleted = true;
                }
            }
            return isDeleted;
        }
        // Legacy overload for backward compatibility
        public async Task<List<ThreadMessage>> GetMessagesAsync(string userId, string threadId)
        {
            return await GetMessagesAsync(userId, threadId, CancellationToken.None);
        }
        // Legacy overload for backward compatibility
        public async Task<bool> DeleteMessages(string userId, string threadId)
        {
            return await DeleteMessages(userId, threadId, CancellationToken.None);
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

        public async Task<bool> UpdateThreadFieldsAsync(string threadId, string userId, Dictionary<string, object> fieldsToUpdate)
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
            Thread thread = await _container.ReadItemAsync<Thread>(threadId, new PartitionKey(userId));
            if (thread == null)
            {
                return false;
            }

            // first get all the associated messages to delete these individually
            var messages = await GetMessagesAsync(userId, threadId);

            bool messagesDeleted = false;
            foreach (ThreadMessage message in messages)
            {
                await _container.DeleteItemAsync<ThreadMessage>(message.Id, new PartitionKey(userId));
                // if for some reason an exception is thrown when deleting the messages,
                // we keep the boolean set to false thus in a next iteration the remaining messages will be deleted
                messagesDeleted = true;
            }

            // if there are no more messages, we can safely delete the thread
            if(messagesDeleted || messages.Count ==0)
                await _container.DeleteItemAsync<Thread>(threadId, new PartitionKey(userId));
            
            return true;

        }

        public async Task<Thread> CreateThreadAsync(string userId)
        {
            var newThread = new Thread
            {
                Id = Guid.NewGuid().ToString(),
                Type = "CHAT_THREAD",
                UserId = userId,
                ThreadName = DateTime.Now.ToString("dd MMM yyyy, HH:mm"),
                Deleted = false //need to be set to false, otherwise the thread will not be returned in the GetThreadsAsync method (and object is not saved properly in cosmosb)
            };

            var response = await _container.CreateItemAsync(newThread, new PartitionKey(userId));
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                throw new Exception("Failed to create a new thread.");
            }
            return response;

        }

        public async Task<List<ThreadMessage>> GetMessagesAsync(string userId, string threadId, CancellationToken cancellationToken)
        {
            var messagesQuery = _container
                .GetItemLinqQueryable<ThreadMessage>(allowSynchronousQueryExecution: true)
                .Where(m => m.ThreadId == threadId)
                .OrderBy(m => m.Created);

            var iterator = messagesQuery.ToFeedIterator();
            var messages = new List<ThreadMessage>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                messages.AddRange(response);
            }
            return messages;
        }

        public async Task<bool> PostMessageAsync(string userId, string threadId, string message, string role, CancellationToken cancellationToken)
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

            var response = await _container.CreateItemAsync(newMessage, new PartitionKey(userId));
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                throw new Exception("Failed to post message.");
            }
            return true;
        }

        public async Task<bool> PostMessageAsync(string userId, ThreadMessage message, CancellationToken cancellationToken)
        {
            var response = await _container.CreateItemAsync(message, new PartitionKey(userId));
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                throw new Exception("Failed to post message.");
            }

            Dictionary<string, object> fieldsToUpdate = new Dictionary<string, object>
            {
                { "lastUpdated", DateTime.UtcNow }
            };

            await UpdateThreadFieldsAsync(message.ThreadId, userId, fieldsToUpdate);

            return true;
        }

        

        public async Task<Thread> GetThreadAsync(string userId, string threadId)
        {
            var messagesQuery = _container
                .GetItemLinqQueryable<Thread>(allowSynchronousQueryExecution: true)
                .Where(m => m.Id == threadId);

            var iterator = messagesQuery.ToFeedIterator();

            var messages = new List<Thread>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                messages.AddRange(response);
            }

            return messages.FirstOrDefault();

        }

    }
}