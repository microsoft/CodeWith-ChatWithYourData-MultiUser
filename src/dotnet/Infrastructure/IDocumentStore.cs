using Domain;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure;

public interface IDocumentStore
{
    Task<DocsPerThread> AddDocumentAsync(string userId, IFormFile document, string fileName, string threadId, string folder);
    Task<bool> DeleteDocumentAsync(string documentName, string folder);
    Task<bool> DocumentExistsAsync(string documentName, string folder);
    Task<IEnumerable<string>> GetDocumentsAsync(string threadId, string folder);
    Task UpdateDocumentAsync(string documentName, string documentUri);
}