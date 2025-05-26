using Microsoft.Azure.Cosmos.Core.Collections;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Graph.Models;
using Microsoft.Graph.Drives.Item.Items.Item.Invite;
using Permission = Microsoft.Graph.Models.Permission;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using System.Text.Json;
using Microsoft.Kiota.Abstractions;
using Microsoft.Graph.Search.Query;
using Microsoft.Graph;
using GraphServiceClient = Microsoft.Graph.GraphServiceClient;
using Microsoft.Kiota.Abstractions.Serialization;
using Infrastructure.Implementations.SPE.Models;
using SharePointOneDriveOptions = Infrastructure.Implementations.SPE.Models.SharePointOneDriveOptions;
using SearchQuery = Infrastructure.Implementations.SPE.Models.SearchQuery;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;

namespace Infrastructure.Implementations.SPE
{
    public class MSGraphService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MSGraphService> _logger;
        private const long SmallFileSizeBoundary = 4000000;
        private const string GraphContainersEndpoint = "beta/storage/fileStorage/containers";
        private readonly GraphServiceClient _graphServiceClient;

        const string graphResource = "https://graph.microsoft.com";

        public MSGraphService(IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<MSGraphService> logger,
            GraphServiceClient graphServiceClient)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _graphServiceClient = graphServiceClient;
           
        }

        public async Task<FileStorageContainer> CreateContainerAsync(string containerName, string containerTypeId, string threadId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating container with Graph SDK: Name={ContainerName}, TypeId={ContainerTypeId}",
                    containerName, containerTypeId);

                // Create base container object
                var container = new FileStorageContainer
                {
                    DisplayName = containerName,
                    ContainerTypeId = Guid.Parse(containerTypeId),
                    Status = FileStorageContainerStatus.Active, // Initially created as inactive
                    Description = !string.IsNullOrEmpty(threadId) ? $"Container associated with thread: {threadId}" : null
                };

                // Create a dictionary for custom properties if needed
                var customProperties = new Dictionary<string, object>();

                // Add threadId as a custom property if provided
                if (!string.IsNullOrEmpty(threadId))
                {
                    customProperties["threadId"] = new
                    {
                        value = threadId,
                        isSearchable = true
                    };
                }

                // Call the Graph API to create the container
                var createdContainer = await _graphServiceClient.Storage.FileStorage.Containers
                    .PostAsync(container, cancellationToken: cancellationToken);

                if (createdContainer == null)
                {
                    throw new InvalidOperationException("Failed to create container: null response received");
                }

                _logger.LogInformation("Successfully created container: ID={ContainerId}, Name={ContainerName}",
                    createdContainer.Id, createdContainer.DisplayName);

                // Activate the container (if desired)
                if (createdContainer.Status != FileStorageContainerStatus.Active)
                {
                    await _graphServiceClient.Storage.FileStorage.Containers[createdContainer.Id]
                        .Activate
                        .PostAsync(cancellationToken: cancellationToken);

                    _logger.LogInformation("Container activated: ID={ContainerId}", createdContainer.Id);
                }

                // todo, get this working with the graphserviceclient
                // await AddCustomPropertyToContainer(container.Id, "threadId", threadId);
                try
                {
                    await UpdateContainerCustomPropertyAsync(createdContainer.Id, "threadId", threadId, cancellationToken);
                }
                catch (Microsoft.Graph.Models.ODataErrors.ODataError ex){
                    _logger.LogWarning(ex, "Error applying property: {Message}", ex.Message);

                }
                return createdContainer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating container: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<bool> UpdateContainerCustomPropertyAsync(string containerId, string propertyName, object propertyValue, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Updating custom property '{PropertyName}' for container {ContainerId} using GraphServiceClient",
                    propertyName, containerId);

                // Create a dictionary to hold the property value and searchable flag
                var customPropertyValue = new Dictionary<string, object>
                {
                    ["value"] = propertyValue,
                    ["isSearchable"] = true
                };

                // Create a request body with AdditionalData to specify the custom property
                var requestBody = new FileStorageContainer
                {
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["customProperties"] = new Dictionary<string, object>
                        {
                            [propertyName] = customPropertyValue
                        }
                    }
                };


                // Use PATCH request to update the container with new custom property
                await _graphServiceClient.Storage.FileStorage.Containers[containerId]
                    .PatchAsync(requestBody, cancellationToken: cancellationToken);

                _logger.LogInformation("Successfully updated custom property '{PropertyName}' for container {ContainerId}",
                    propertyName, containerId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating custom property '{PropertyName}' for container {ContainerId}",
                    propertyName, containerId);
                throw;
            }
        }


        public async Task<IEnumerable<FileStorageContainer>> GetContainersByThreadId(string threadId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting containers by threadId using GraphServiceClient: {ThreadId}", threadId);

                // Use OData filter to query containers where the threadId custom property equals the provided threadId
                var options = new Microsoft.Graph.Storage.FileStorage.Containers.ContainersRequestBuilder.ContainersRequestBuilderGetQueryParameters
                {
                    Filter = $"customProperties/threadId/value eq '{threadId}'"
                };

                // Get containers with the specified filter
                var containers = await _graphServiceClient.Storage.FileStorage.Containers
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters = options;
                    }, cancellationToken);

                if (containers?.Value == null)
                {
                    _logger.LogWarning("No containers found with threadId: {ThreadId} or empty response", threadId);
                    return Enumerable.Empty<FileStorageContainer>();
                }

                _logger.LogInformation("Successfully retrieved {Count} containers for threadId: {ThreadId}",
                    containers.Value, threadId);
                return containers.Value!;
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "GraphServiceClient error getting containers by threadId: {Status} - {Message}",
                    ex.ResponseStatusCode, ex.Message);
                return Enumerable.Empty<FileStorageContainer>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting containers by threadId with GraphServiceClient: {ThreadId}", threadId);
                return Enumerable.Empty<FileStorageContainer>();
            }
        }

        public async Task<Drive> GetDriveAsync(string driveId)
        {
           // var graphServiceClient = getGraphClient(accessToken);
            Drive drive = await _graphServiceClient.Drives[driveId].GetAsync();
            return drive;
        }

        public async Task<DriveItem> GetDriveRoot(string driveId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting drive root with GraphServiceClient for drive {DriveId}", driveId);

            try
            {
                // Use the GraphServiceClient to get the drive root
                var driveItem = await _graphServiceClient.Drives[driveId].Root
                    .GetAsync(cancellationToken: cancellationToken);

                if (driveItem != null)
                {
                    _logger.LogInformation("Successfully retrieved drive root for drive {DriveId}", driveId);
                    return driveItem;
                }
                else
                {
                    throw new InvalidOperationException($"Failed to retrieve drive root for drive {driveId}");
                }
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "GraphServiceClient error getting drive root: {Status} - {Message}",
                    ex.ResponseStatusCode, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drive root with GraphServiceClient for driveId {DriveId}", driveId);
                throw;
            }
        }

        public async Task<ICollection<DriveItem>> GetDriveRootItems(string driveId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting drive root items with GraphServiceClient for drive {DriveId}", driveId);

            try
            {
                // Use the GraphServiceClient to get children of the drive root
                var result = await _graphServiceClient.Drives[driveId].Items["root"].Children.GetAsync();
               // var response = await _graphServiceClient.Drives[driveId].Root.ItemWithPath("root").GetAsync(cancellationToken: cancellationToken);

                if (result?.Value != null)
                {
                    _logger.LogInformation("Successfully retrieved {Count} items from drive root for drive {DriveId}",
                        result.Value.Count, driveId);
                    return result.Value;
                }
                else
                {
                    _logger.LogWarning("No items found in drive root or empty response for drive {DriveId}", driveId);
                    return new List<DriveItem>();
                }
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "GraphServiceClient error getting drive root items: {Status} - {Message}",
                    ex.ResponseStatusCode, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drive root items with GraphServiceClient for driveId {DriveId}", driveId);
                throw;
            }
        }

        public async Task<DriveItem> GetDriveItem(string driveId, string id)
        {
            var driveItem = await _graphServiceClient.Drives[driveId].Items[id].GetAsync();
            return driveItem;
        }

        public async Task<string> GetItemPreview(string driveId, string itemId)
        {
            ItemPreviewInfo preview = await _graphServiceClient
                .Drives[driveId]
                .Items[itemId]
                .Preview
                .PostAsync(
                new Microsoft.Graph.Drives.Item.Items.Item.Preview.PreviewPostRequestBody(),
                null,
                new CancellationToken()
                );
            return preview.GetUrl;
        }

        public async Task<DriveItem> UpdateDriveItem(string driveId, string itemId, DriveItem driveItem)
        {
            return _graphServiceClient.Drives[driveId].Items[itemId].PatchAsync(driveItem).Result;
        }

        public async Task<DriveItem> CopyDriveItem(string accessToken, string driveId, string itemId, string name, ItemReference parentReference)
        {
            await Task.CompletedTask;
            return new DriveItem();
            //var graphServiceClient = getGraphClient(accessToken);
            //return await graphServiceClient.Drives[driveId].Items[itemId]
            //    .Copy(name, parentReference)
            //    .Request()
            //    .PostAsync();
        }

        public async Task<DriveItem> AddFile(string accessToken, string driveId, string parentId, string name, Stream stream)
        {
            //Upload small file> https://docs.microsoft.com/en-us/graph/api/driveitem-put-content
            //Upload large file> https://docs.microsoft.com/en-us/graph/sdks/large-file-upload

            return await UploadSmallFile(driveId, parentId, name, stream);
        }

        public async Task<DriveItem> UploadSmallFile(
     string driveId,
     string parentId,
     string name,
     Stream stream,
     CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting small file upload with GraphServiceClient: {Name} to drive {DriveId}, parent folder {ParentId}",
                name, driveId, parentId);

            try
            {
                // Make sure stream is positioned at the beginning
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }

                // Use the GraphServiceClient to upload the file
                // The path is constructed as /drives/{driveId}/items/{parentId}:/{fileName}:/content
                var driveItem = await _graphServiceClient.Drives[driveId].Items[parentId].ItemWithPath(name).Content
                    .PutAsync(stream, cancellationToken: cancellationToken);

                if (driveItem != null)
                {
                    _logger.LogInformation("Successfully uploaded file with GraphServiceClient: {Name}, Item ID: {ItemId}",
                        name, driveItem.Id);
                    return driveItem;
                }
                else
                {
                    throw new InvalidOperationException("Upload succeeded but no DriveItem was returned");
                }
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "GraphServiceClient error uploading file: {Status} - {Message}",
                    ex.ResponseStatusCode, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file with GraphServiceClient: {FileName}", name);
                throw;
            }
        }
        //public async Task<DriveItem> UploadSmallFile(string accessToken, string driveId, string parentId, string name, Stream stream)
        //{
        //    _logger.LogInformation("Starting small file upload with direct API: {Name} to drive {DriveId}, parent folder {ParentId}",
        //        name, driveId, parentId);

        //    try
        //    {
        //        // Create HttpClient and set authorization header
        //        using var httpClient = new HttpClient();
        //        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        //        // Prepare the URL with conflict behavior parameter
        //        string apiUrl = $"https://graph.microsoft.com/v1.0/drives/{driveId}/items/{parentId}:/{name}:/content?@microsoft.graph.conflictBehavior=rename";

        //        // Read the stream content into a byte array
        //        using var memoryStream = new MemoryStream();
        //        await stream.CopyToAsync(memoryStream);
        //        byte[] fileData = memoryStream.ToArray();

        //        // Create the HTTP content from the file data
        //        using var content = new ByteArrayContent(fileData);

        //        // Send the PUT request to upload the file
        //        var response = await httpClient.PutAsync(apiUrl, content);

        //        // Check if the request was successful
        //        if (!response.IsSuccessStatusCode)
        //        {
        //            string errorContent = await response.Content.ReadAsStringAsync();
        //            _logger.LogError("Error uploading file: Status code {StatusCode}, Content: {ErrorContent}",
        //                response.StatusCode, errorContent);
        //            throw new HttpRequestException($"Error uploading file: {response.StatusCode}");
        //        }

        //        // Deserialize the response to get the created DriveItem
        //        string responseJson = await response.Content.ReadAsStringAsync();
        //        var driveItem = JsonConvert.DeserializeObject<DriveItem>(responseJson);

        //        _logger.LogInformation("Successfully uploaded file with direct API: {Name}, Item ID: {ItemId}",
        //            name, driveItem?.Id);

        //        return driveItem;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error uploading file with direct API: {FileName}", name);
        //        throw;
        //    }
        //}


        /// <summary>
        /// Uploads a large file (4MB or larger) to OneDrive/SharePoint using Microsoft Graph API upload session
        /// </summary>
        /// <summary>
        /// Uploads a large file (4MB or larger) to OneDrive/SharePoint using direct HTTP requests to Microsoft Graph API
        /// </summary>
        private async Task<DriveItem> UploadLargeFile(string accessToken, string driveId, string parentId, string name, Stream fileStream)
        {
            _logger.LogInformation("Starting large file upload with direct HTTP: {Name}", name);

            try
            {
                // Get the access token from the graph client
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Step 1: Create an upload session
                string createSessionUrl = $"https://graph.microsoft.com/beta/drives/{driveId}/items/{parentId}:/{name}:/createUploadSession";
                var requestBody = new
                {
                    item = new
                    {
                        name,
                        AdditionalData = new Dictionary<string, object>
                        {
                            { "@microsoft.graph.conflictBehavior", "rename" }
                        }
                    }
                };

                StringContent content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage sessionResponse = await client.PostAsync(createSessionUrl, content);

                if (!sessionResponse.IsSuccessStatusCode)
                {
                    string errorContent = await sessionResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Error creating upload session: {StatusCode}, {ErrorContent}",
                        sessionResponse.StatusCode, errorContent);
                    throw new HttpRequestException($"Error creating upload session: {sessionResponse.StatusCode}");
                }

                // Parse the upload session from the response
                string sessionResponseContent = await sessionResponse.Content.ReadAsStringAsync();
                var sessionInfo = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(sessionResponseContent);
                string uploadUrl = sessionInfo.GetProperty("uploadUrl").GetString();

                if (string.IsNullOrEmpty(uploadUrl))
                    throw new InvalidOperationException("Failed to get upload URL from session response");

                _logger.LogInformation("Created upload session: {UploadUrl}", uploadUrl);

                // The recommended fragment size is between 5-10 MiB
                int maxSliceSize = 5 * 1024 * 1024; // 5 MB
                long totalFileSize = fileStream.Length;
                long totalBytesUploaded = 0;

                // Reset the stream position
                fileStream.Position = 0;

                // Continue uploading until we've sent the entire file
                while (totalBytesUploaded < totalFileSize)
                {
                    // Calculate the size of this slice
                    int bytesToRead = (int)Math.Min(
                        maxSliceSize,
                        totalFileSize - totalBytesUploaded);

                    // Read that many bytes from the stream
                    byte[] buffer = new byte[bytesToRead];
                    int bytesRead = await fileStream.ReadAsync(buffer, 0, bytesToRead);

                    // If we couldn't read as many bytes as expected, the stream might have ended early
                    if (bytesRead == 0)
                        break;

                    // Create the content range header value
                    long rangeStart = totalBytesUploaded;
                    long rangeEnd = totalBytesUploaded + bytesRead - 1;
                    string contentRange = $"bytes {rangeStart}-{rangeEnd}/{totalFileSize}";

                    // Log the upload progress
                    double progressPercentage = (double)totalBytesUploaded / totalFileSize * 100;
                    _logger.LogInformation("Uploading slice: {RangeStart}-{RangeEnd}/{TotalSize} ({ProgressPercentage:F2}%)",
                        rangeStart, rangeEnd, totalFileSize, progressPercentage);

                    // Create the HTTP request for this slice
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
                    request.Headers.Add("Content-Range", contentRange);
                    request.Content = new ByteArrayContent(buffer, 0, bytesRead);

                    // Send the request
                    HttpResponseMessage response = await client.SendAsync(request);

                    // Handle the response
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        totalBytesUploaded += bytesRead;

                        // If we've uploaded the entire file, the response should contain the item metadata
                        if (totalBytesUploaded >= totalFileSize)
                        {
                            DriveItem driveItem = System.Text.Json.JsonSerializer.Deserialize<DriveItem>(responseContent);
                            _logger.LogInformation("Successfully uploaded large file: {Name}, Item ID: {ItemId}",
                                name, driveItem?.Id);
                            return driveItem;
                        }
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError("Error uploading file slice: {StatusCode}, {ErrorContent}",
                            response.StatusCode, errorContent);
                        throw new HttpRequestException($"Error uploading file slice: {response.StatusCode}");
                    }
                }

                throw new InvalidOperationException("Upload completed without receiving final DriveItem response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading large file {FileName}", name);
                throw;
            }
        }

        public async Task DeleteDriveItem(string driveId, string itemId)
        {
            await _graphServiceClient.Drives[driveId].Items[itemId]
                .DeleteAsync();
        }


        public async Task AddFolder(string driveId, string parentId, string name)
        {
            var driveItem = new DriveItem
            {
                Name = name,
                Folder = new Folder
                {
                },
                AdditionalData = new Dictionary<string, object>()
                {
                    {"@microsoft.graph.conflictBehavior", "rename"}
                }
            };

            try
            {
                await _graphServiceClient.Drives[driveId].Items[parentId].Children
                        .PostAsync(driveItem);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task<ICollection<Permission>> GetPermissions(string driveId, string itemId)
        {
            var permissions = await _graphServiceClient.Drives[driveId].Items[itemId].Permissions.GetAsync();
            return permissions.Value;
        }

        public async Task<Permission> GetPermission(string driveId, string itemId, string permissionId)
        {
            var permission = await _graphServiceClient.Drives[driveId].Items[itemId].Permissions[permissionId]
                .GetAsync();
            return permission;
        }

        // Graph reference on permision update
        // https://learn.microsoft.com/en-us/graph/api/permission-update?view=graph-rest-beta
        //
        public async Task UpdatePermission(string driveId, string itemId, string permissionId, List<string> roles)
        {
            var permission = new Permission
            {
                Roles = roles
            };

            await _graphServiceClient.Drives[driveId].Items[itemId].Permissions[permissionId].PatchAsync(permission);
        }

        // Graph reference on permision delete
        // https://learn.microsoft.com/en-us/graph/api/permission-delete?view=graph-rest-beta
        //
        public async Task DeletePermission(string driveId, string itemId, string permissionId)
        {
            await _graphServiceClient.Drives[driveId].Items[itemId].Permissions[permissionId]
                .DeleteAsync();
        }


        // Graph reference on permisions
        // https://docs.microsoft.com/en-us/graph/api/driveitem-invite
        public async Task AddPermissions(string driveId, string itemId, IEnumerable<string> roles, IEnumerable<DriveRecipient> recipients)
        {

            var requireSignIn = true;
            var sendInvitation = false;
            InvitePostRequestBody invitePostRequestBody = new InvitePostRequestBody();
            invitePostRequestBody.Roles = roles.ToList();
            invitePostRequestBody.Recipients = recipients.ToList();
            invitePostRequestBody.RequireSignIn = requireSignIn;
            invitePostRequestBody.SendInvitation = sendInvitation;


            await _graphServiceClient.Drives[driveId].Items[itemId]
                 .Invite.PostAsInvitePostResponseAsync(invitePostRequestBody);


        }

        public async Task<Uri> GetFileDownloadUrl(string driveId, string itemId)
        {
            try
            {
                // Use the GraphServiceClient to get the download URL
                var driveItem = await _graphServiceClient.Drives[driveId].Items[itemId]
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = new[] { "@microsoft.graph.downloadUrl" };
                    });

                // Check if the download URL is available
                if (driveItem?.AdditionalData != null && driveItem.AdditionalData.TryGetValue("@microsoft.graph.downloadUrl", out var downloadUrl))
                {
                    return new Uri(downloadUrl.ToString());
                }

                throw new InvalidOperationException("Download URL not found for the specified file.");
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Error retrieving file download URL for driveId: {DriveId}, itemId: {ItemId}", driveId, itemId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving file download URL for driveId: {DriveId}, itemId: {ItemId}", driveId, itemId);
                throw;
            }
        }

        

        /// <summary>
        /// Searches for drive items using the Microsoft Graph SDK search API.
        /// This is using the custom sendrequest since the Graph SDK doesn't support the new SharePointOneDriveOptions { IncludeHiddenContent = true } yet, 
        /// which makes it impossible not query for documents in SPE
        /// </summary>
        /// <param name="searchString">The search query string</param>
        /// <param name="containerTypeId">Optional container type ID to filter results</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A list of DriveItems matching the search criteria</returns>
        public async Task<List<DriveItem>> SearchDriveItems(
            string searchString,
            string? containerTypeId = null,
            int maxResults = 100,
            CancellationToken cancellationToken = default)
        {

            try
            {
                // Build the query string
                string queryString = !string.IsNullOrEmpty(containerTypeId)
                    ? $"({searchString}) AND (containerTypeId:{containerTypeId})"
                    : searchString;

                _logger.LogInformation("Searching drive items with GraphServiceClient. Query: '{SearchString}'", queryString);

                var requestBody = new SearchRequestBody
                {
                    Requests = new List<SearchRequestItem>
                    {
                        new SearchRequestItem
                        {
                            EntityTypes = new List<string> { "driveItem" },
                            Query = new SearchQuery { QueryString = queryString },
                            Size = maxResults,
                            SharePointOneDriveOptions = new SharePointOneDriveOptions { IncludeHiddenContent = true },
                            From = 0,
                            Fields = new List<string> { "id", "name", "createdDateTime", "lastModifiedDateTime",
                                                      "size", "threadIdOWSTEXT", "parentReference" }
                        }
                    }
                };

                // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=csharp
                var response = await SendCustomRequestAsync("/search/query", requestBody, HttpMethod.Post, cancellationToken);

                // Parse the response first to get the "value" array
                var jsonDocument = JsonDocument.Parse(response.GetRawText());
                var valueArray = jsonDocument.RootElement.GetProperty("value");

                // Process search results and extract DriveItems
                var driveItems = new List<DriveItem>();

                // Navigate through the response structure
                // Process each search result in the value array
                foreach (var searchResultElement in valueArray.EnumerateArray())
                {
                    try
                    {
                        // Check if we have hitsContainers property
                        if (!searchResultElement.TryGetProperty("hitsContainers", out var hitsContainersElement) ||
                            hitsContainersElement.ValueKind != JsonValueKind.Array)
                        {
                            continue; // Skip if no valid hits containers
                        }

                        // Process each hits container
                        foreach (var hitsContainer in hitsContainersElement.EnumerateArray())
                        {
                            // Get hits array from the container
                            if (!hitsContainer.TryGetProperty("hits", out var hitsElement) ||
                                hitsElement.ValueKind != JsonValueKind.Array)
                            {
                                continue; // Skip if no valid hits
                            }

                            // Process each individual hit
                            foreach (var hit in hitsElement.EnumerateArray())
                            {
                                try
                                {
                                    int rank = hit.TryGetProperty("rank", out var rankElement) &&
                                        rankElement.ValueKind == JsonValueKind.Number ?
                                        rankElement.GetInt32() : 0;

                                    // Get the resource object that contains the DriveItem
                                    if (!hit.TryGetProperty("resource", out var resourceElement) ||
                                        resourceElement.ValueKind != JsonValueKind.Object)
                                    {
                                        continue; // Skip invalid resources
                                    }

                                    // Create a new DriveItem and populate from the JSON
                                    var driveItem = new DriveItem
                                    {
                                        // Initialize AdditionalData to store hit info and other metadata
                                        AdditionalData = new Dictionary<string, object>()
                                    };

                                    // Extract basic properties
                                    if (resourceElement.TryGetProperty("id", out var idElement))
                                        driveItem.Id = idElement.GetString();

                                    if (resourceElement.TryGetProperty("name", out var nameElement))
                                        driveItem.Name = nameElement.GetString();

                                    if (resourceElement.TryGetProperty("size", out var sizeElement) &&
                                        sizeElement.ValueKind == JsonValueKind.Number)
                                        driveItem.Size = sizeElement.GetInt64();

                                    if (resourceElement.TryGetProperty("createdDateTime", out var createdElement))
                                        driveItem.CreatedDateTime = createdElement.GetDateTimeOffset();

                                    if (resourceElement.TryGetProperty("lastModifiedDateTime", out var modifiedElement))
                                        driveItem.LastModifiedDateTime = modifiedElement.GetDateTimeOffset();

                                    // Extract parentReference if it exists
                                    if (resourceElement.TryGetProperty("parentReference", out var parentElement) &&
                                        parentElement.ValueKind == JsonValueKind.Object)
                                    {
                                        var parentRef = new ItemReference();

                                        if (parentElement.TryGetProperty("driveId", out var driveIdElement))
                                            parentRef.DriveId = driveIdElement.GetString();

                                        if (parentElement.TryGetProperty("id", out var parentIdElement))
                                            parentRef.Id = parentIdElement.GetString();

                                        driveItem.ParentReference = parentRef;
                                    }

                                    // Extract listItem fields if available (particularly threadId)
                                    if (resourceElement.TryGetProperty("listItem", out var listItemElement) &&
                                        listItemElement.ValueKind == JsonValueKind.Object &&
                                        listItemElement.TryGetProperty("fields", out var fieldsElement) &&
                                        fieldsElement.ValueKind == JsonValueKind.Object)
                                    {
                                        // Look for threadIdOWSTEXT or any other specific fields
                                        if (fieldsElement.TryGetProperty("threadIdOWSTEXT", out var threadIdElement))
                                        {
                                            string threadId = threadIdElement.GetString();
                                            driveItem.AdditionalData["threadId"] = threadId;
                                            _logger.LogDebug("Found threadId: {ThreadId} for file: {FileName}",
                                                threadId, driveItem.Name);
                                        }

                                        // Optionally store the listItem ID
                                        if (listItemElement.TryGetProperty("id", out var listItemIdElement))
                                            driveItem.AdditionalData["listItemId"] = listItemIdElement.GetString();
                                    }

                                    // Add to results
                                    driveItems.Add(driveItem);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error processing individual search hit: {Message}", ex.Message);
                                    // Continue with other hits
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing search result: {Message}", ex.Message);
                        // Continue with other search results
                    }
                }

                _logger.LogInformation("Search completed successfully with direct JSON parsing. Found {Count} items.", driveItems.Count);
                return driveItems;

            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "GraphServiceClient error searching drive items: {Status} - {Message}",
                    ex.ResponseStatusCode, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching drive items with GraphServiceClient: {Message}", ex.Message);
                throw;
            }
        }

        // this is a custom request implementation to the Graph API using the GraphServiceClient
        internal async Task<JsonElement> SendCustomRequestAsync(
            string endpoint,
            object requestBody,
            HttpMethod method = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sending custom request to endpoint: {Endpoint}", endpoint);

                if (_graphServiceClient.RequestAdapter is not Microsoft.Kiota.Http.HttpClientLibrary.HttpClientRequestAdapter httpAdapter)
                {
                    throw new InvalidOperationException("GraphServiceClient is not using HttpClientRequestAdapter");
                }

                // Default to POST if not specified
                method ??= HttpMethod.Post;

                // Create a request information object
                var requestInfo = new Microsoft.Kiota.Abstractions.RequestInformation
                {
                    HttpMethod = Method.POST,
                    URI = new Uri($"{httpAdapter.BaseUrl?.TrimEnd('/')}/{endpoint.TrimStart('/')}"),
                };

                // Serialize the request body using the client's serializer
                string contentType = "application/json";

                // Ensure the requestBody is cast to IParsable before calling SetContentFromParsable
                if (requestBody is IParsable parsableRequestBody)
                {
                    requestInfo.SetContentFromParsable(
                        httpAdapter,
                        contentType,
                        parsableRequestBody);
                }
                else
                {
                    throw new InvalidOperationException("The requestBody must implement IParsable.");
                }

                // Send the request via the adapter
                using var stream = await httpAdapter.SendPrimitiveAsync<Stream>(
                    requestInfo,
                    cancellationToken: cancellationToken);

                // Parse the response
                if (stream != null)
                {
                    using var jsonDoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                    return jsonDoc.RootElement.Clone();
                }

                return new JsonElement();
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "GraphServiceClient error sending custom request: {Status} - {Message}",
                    ex.ResponseStatusCode, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending custom request: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Searches for drive items using the Microsoft Graph search API with direct HTTP calls.
        /// </summary>
        /// <param name="accessToken">The access token for authentication</param>
        /// <param name="searchString">The search query string</param>
        /// <param name="containerTypeId">Optional container type ID to filter results (defaults to null)</param>
        /// <param name="maxResults">Maximum number of results to return (defaults to 100)</param>
        /// <returns>A list of DriveItems matching the search criteria</returns>
        //public async Task<List<DriveItem>> SearchDriveItems(string accessToken, string searchString, string containerTypeId = null, int maxResults = 100)
        //{
        //    _logger.LogInformation("Searching drive items with direct API. Query: '{SearchString}'", searchString);

        //    try
        //    {
        //        // Create the request URL
        //        string endpoint = "https://graph.microsoft.com/beta/search/query";

        //        // Build the query string
        //        string queryString = !string.IsNullOrEmpty(containerTypeId)
        //            ? $"({searchString}) AND (containerTypeId:{containerTypeId})"
        //            : searchString;

        //        // Create the request payload
        //        var requestBody = new
        //        {
        //            requests = new[]
        //            {
        //                new
        //                {
        //                    entityTypes = new[] { "driveItem" },
        //                    query = new
        //                    {
        //                        queryString
        //                    },
        //                    size = maxResults,
        //                    sharePointOneDriveOptions = new
        //                    {
        //                        includeHiddenContent = true
        //                    },
        //                    from = 0,
        //                    fields =  new [] { "id", "name", "createdDateTime", "lastModifiedDateTime", "size", "threadIdOWSTEXT", "parentReference" }
        //                }
        //            }
        //        };

        //        // Serialize the request body
        //        string jsonRequest = JsonConvert.SerializeObject(requestBody);

        //        // Create HTTP client and set headers
        //        using var httpClient = new HttpClient();
        //        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        //        // Send the POST request
        //        using var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        //        var response = await httpClient.PostAsync(endpoint, content);

        //        // Check if request was successful
        //        if (!response.IsSuccessStatusCode)
        //        {
        //            string errorContent = await response.Content.ReadAsStringAsync();
        //            _logger.LogError("Search request failed. Status: {StatusCode}, Error: {Error}",
        //                response.StatusCode, errorContent);
        //            throw new HttpRequestException($"Search request failed with status code {response.StatusCode}");
        //        }

        //        // Parse the response
        //        string responseContent = await response.Content.ReadAsStringAsync();
        //        JObject responseObject = JObject.Parse(responseContent);

        //        // Extract drive items from the search results
        //        var driveItems = new List<DriveItem>();

        //        // Navigate through the response structure
        //        var valueArray = responseObject["value"] as JArray;
        //        if (valueArray != null && valueArray.Count > 0)
        //        {
        //            var hitsContainers = valueArray[0]["hitsContainers"] as JArray;
        //            if (hitsContainers != null && hitsContainers.Count > 0)
        //            {
        //                var hits = hitsContainers[0]["hits"] as JArray;
        //                if (hits != null)
        //                {
        //                    foreach (var hit in hits)
        //                    {
        //                        // Extract the DriveItem from the resource property
        //                        var resourceObject = hit["resource"];
        //                        if (resourceObject != null && resourceObject["@odata.type"]?.ToString() == "#microsoft.graph.driveItem")
        //                        {
        //                            try
        //                            {
        //                                // Convert to DriveItem
        //                                var driveItem = resourceObject.ToObject<DriveItem>();

        //                                // Ensure AdditionalData dictionary exists
        //                                if (driveItem.AdditionalData == null)
        //                                {
        //                                    driveItem.AdditionalData = new Dictionary<string, object>();
        //                                }

        //                                // Extract listItem fields if they exist
        //                                if (resourceObject["listItem"] != null &&
        //                                    resourceObject["listItem"]["fields"] != null)
        //                                {
        //                                    var fields = resourceObject["listItem"]["fields"];

        //                                    // Add threadId to AdditionalData if it exists
        //                                    if (fields["threadIdOWSTEXT"] != null)
        //                                    {
        //                                        string threadId = fields["threadIdOWSTEXT"].ToString();
        //                                        driveItem.AdditionalData["threadId"] = threadId;

        //                                        // Log for debugging
        //                                        _logger.LogDebug("Found threadId: {ThreadId} for file: {FileName}",
        //                                            threadId, driveItem.Name);
        //                                    }

        //                                    // Store the entire fields object for completeness
        //                                    driveItem.AdditionalData["listItemFields"] = fields.ToObject<Dictionary<string, object>>();

        //                                    // If there's a listItem ID, store it too
        //                                    if (resourceObject["listItem"]["id"] != null)
        //                                    {
        //                                        driveItem.AdditionalData["listItemId"] = resourceObject["listItem"]["id"].ToString();
        //                                    }
        //                                }

        //                                // Store the hit ID and rank for reference
        //                                if (hit["hitId"] != null)
        //                                {
        //                                    driveItem.AdditionalData["hitId"] = hit["hitId"].ToString();
        //                                }

        //                                if (hit["rank"] != null && hit["rank"].Type == JTokenType.Integer)
        //                                {
        //                                    driveItem.AdditionalData["searchRank"] = hit["rank"].Value<int>();
        //                                }

        //                                driveItems.Add(driveItem);
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                _logger.LogError(ex, "Error processing search hit: {Message}", ex.Message);
        //                                // Continue processing other hits even if one fails
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        _logger.LogInformation("Search completed successfully. Found {Count} items.", driveItems.Count);
        //        return driveItems;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error searching drive items: {Message}", ex.Message);
        //        throw;
        //    }
        //}

    }
}
