using Domain.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Interfaces;

public interface IDocumentRegistry
{
    Task<string> AddDocumentToThreadAsync(DocsPerThread docsPerThread);
    Task<string> UpdateDocumentAsync(DocsPerThread docsPerThread);
    Task<bool> RemoveDocumentFromThreadAsync(List<DocsPerThread> docsPerThread); // this is the soft delete method for an entire thread
    Task<bool> RemoveDocumentAsync(DocsPerThread document);// this is the soft delete method for a single document
    Task<bool> DeleteDocumentAsync(DocsPerThread document); // this is the hard delete method for a single document
    Task<List<DocsPerThread>> GetDocsPerThreadAsync(string threadId);
    Task<DocsPerThread> GetDocPerThreadAsync(string threadId, string documentId);
    Task<string> GetFolderForThreadAsync(string threadId); // this is to determine the folder or driveId for a thread
}