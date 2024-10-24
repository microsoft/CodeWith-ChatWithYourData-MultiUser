using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure;

public interface IDocumentRegistry
{
    Task<string> AddDocumentToThreadAsync(DocsPerThread docsPerThread);
    Task<string> UpdateDocumentAsync(DocsPerThread docsPerThread);
    Task<bool> RemoveDocumentFromThreadAsync(List<DocsPerThread> docsPerThread); // this is the soft delete method for an entire thread
    Task<bool> RemoveDocumentAsync(DocsPerThread document);// this is the soft delete method for a single document
    Task<bool> DeleteDocumentAsync(DocsPerThread document); // this is the hard delete method for a single document
    Task<List<DocsPerThread>> GetDocsPerThreadAsync(string threadId);
}