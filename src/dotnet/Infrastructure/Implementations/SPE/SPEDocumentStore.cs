using Domain.Cosmos;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Identity.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Implementations.SPE
{
    public class SPEDocumentStore : IDocumentStore
    {
        private readonly ILogger<SPEDocumentStore> _logger;
        private readonly IConfiguration _configuration;
        private readonly MSGraphService _graphService;

        private string containerTypeId;
        private string tenantId;

        public SPEDocumentStore(ILogger<SPEDocumentStore> logger, 
            IConfiguration configuration,
            MSGraphService graphService,
            ITokenAcquisition tokenAcquisition)
        {
            _logger = logger;
            _configuration = configuration;
            _graphService = graphService;
            tenantId = _configuration.GetValue<string>("SPE:TenantId") ?? "f903e023-a92d-4561-9a3b-d8429e3fa1fd";
            containerTypeId = _configuration.GetValue<string>("SPE:ContainerTypeId") ?? "aaace084-a939-40a0-98f0-919307b365ab";
        }

        public async Task<DocsPerThread> AddDocumentAsync(string userId, IFormFile document, string fileName, string threadId, string folder)
        {
            //create a container for the thread first, if there's no container, we need to provision that.
            var driveId = await GetOrCreateDrive(folder, threadId);

            var documentId = Guid.NewGuid().ToString();
            var uploadedFile = await _graphService.AddFile(driveId:driveId.Id, accessToken: string.Empty, stream: document.OpenReadStream(), parentId:"root", name: fileName);

            //then create a document in the cosmos db with the metadata of the document
            DocsPerThread docsPerThread = new()
            {
                Id = uploadedFile.Id!.ToString(),
                ThreadId = threadId,
                DocumentName = fileName,
                UserId = userId,
                Folder = driveId.Id,
                FileSize = document.Length,
                UploadDate = DateTime.Now
            };

            return docsPerThread;
        }

        internal async Task<Drive> GetOrCreateDrive(string driveId, string threadId)
        {
            if (!string.IsNullOrEmpty(driveId))
                return await _graphService.GetDriveAsync(driveId);
          
            var container = await _graphService.CreateContainerAsync(containerName:threadId, containerTypeId, threadId:threadId);
            return await _graphService.GetDriveAsync(container.Id);
          
        }

        public async Task<bool> DeleteDocumentAsync(string documentName, string folder)
        {
            bool isDeleted = false;
            try
            {
                var driveItem = await _graphService.GetDriveItem(folder, documentName);
                if (driveItem != null)
                {
                    await _graphService.DeleteDriveItem(folder, driveItem.Id);
                    isDeleted = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentName} in folder {Folder}", documentName, folder);
            }
            return isDeleted;
        }

        public async Task<bool> DocumentExistsAsync(string documentName, string folder)
        {
            bool itemExists = false;
            try
            {
                var driveItem = await _graphService.GetDriveItem(folder, documentName);
                itemExists = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching document {DocumentName} in folder {Folder}", documentName, folder);
            }
            return itemExists;
        }

        public async Task<IEnumerable<DocsPerThread>> GetAllDocumentsAsync(string folder)
        {
            var results = new List<DocsPerThread>();
            var documents = await _graphService.SearchDriveItems("isDocument:true", containerTypeId);
            foreach (var driveItem in documents)
            {
                string threadId = string.Empty;
                if (driveItem.AdditionalData.TryGetValue("threadId", out var threadIdValue))
                {
                    threadId = threadIdValue.ToString();
                }

                DocsPerThread docsPerThread = new DocsPerThread()
                {
                    DocumentName = driveItem.Name!,
                    Folder = driveItem.ParentReference.DriveId,
                    FileSize = driveItem.Size ?? 0,
                    ThreadId = threadId,
                    UploadDate = driveItem.LastModifiedDateTime?.DateTime ?? DateTime.MinValue, // Fix for CS0029
                    UserId = string.Empty,
                    Id = driveItem.Id!,
                };

                results.Add(docsPerThread);
            }

            return results;
        }

        public async Task<IEnumerable<string>> GetDocumentsAsync(string threadId, string folder)
        {
            var results = new List<string>();
            var documents = await _graphService.GetDriveRootItems(folder);
            foreach (var document in documents)
            {
                results.Add(document.Name);
            }

            return results;
        }

        public Task UpdateDocumentAsync(string documentName, string documentUri)
        {
            throw new NotImplementedException();
        }
    }
}
