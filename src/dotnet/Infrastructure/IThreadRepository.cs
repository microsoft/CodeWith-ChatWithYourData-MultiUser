using Domain;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Infrastructure;
public interface IThreadRepository
{
    Task<List<Domain.Thread>> GetThreadsAsync(string userId);
    Task<Domain.Thread> CreateThreadAsync(string userId);
    Task<bool> DeleteThreadAsync(string userId, string threadId);
    Task<List<ThreadMessage>> GetMessagesAsync(string userId, string threadId);
    Task<bool> PostMessageAsync(string userId, string threadId, string message, string role);
    List<ThreadMessage> GetAllThreads(DateTime expirationDate);
    List<string> GetAllThreadIds(DateTime expirationDate);
    Task<bool> MarkThreadAsDeletedAsync(string userId, string threadId);
}