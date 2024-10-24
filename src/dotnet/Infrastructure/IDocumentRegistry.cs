﻿using Domain;
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
    Task<bool> RemoveDocumentFromThreadAsync(List<DocsPerThread> docsPerThread);
    Task<bool> RemoveDocumentAsync(DocsPerThread document);
    Task<List<DocsPerThread>> GetDocsPerThreadAsync(string threadId);
}