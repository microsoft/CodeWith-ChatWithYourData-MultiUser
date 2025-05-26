using Domain.Chat;
using Domain.Cosmos;
using Domain.Search;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Thread = Domain.Cosmos.Thread;

namespace WebApi.Services
{
    public class AdminService
    {
        private readonly ILogger<AdminService> _logger;
        private readonly IDocumentStore _documentStore;
        private readonly IDocumentRegistry _documentRegistry;
        private readonly IThreadRepository _threadRepository;
        private readonly ISearchService _searchService;
        private readonly IAIService _aiService;
        private readonly IConfiguration _configuration;
        private readonly ThreadSafeSettings _settings;
        private readonly string _containerName;

        public AdminService(
            ILogger<AdminService> logger,
            IDocumentStore documentStore,
            IDocumentRegistry documentRegistry,
            IThreadRepository threadRepository,
            ISearchService searchService,
            IAIService aiService,
            IConfiguration configuration,
            ThreadSafeSettings settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
            _documentRegistry = documentRegistry ?? throw new ArgumentNullException(nameof(documentRegistry));
            _threadRepository = threadRepository ?? throw new ArgumentNullException(nameof(threadRepository));
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _containerName = _configuration.GetValue<string>("Storage:ContainerName") ?? "documents";
            _logger.LogInformation("AdminService initialized successfully");
        }

        public Settings GetSettings()
        {
            _logger.LogInformation("Fetching application settings");
            return _settings.GetSettings();
        }

        public Settings UpdateSettings(Settings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings), "Settings payload is required");
            }

            _logger.LogInformation("Updating application settings");
            _settings.UpdateSettings(settings);
            _logger.LogInformation("Settings updated successfully");
            
            return _settings.GetSettings();
        }

        public async Task<IEnumerable<DocsPerThread>> GetAllDocumentsAsync()
        {
            _logger.LogInformation("Fetching all documents from store '{ContainerName}'", _containerName);
            var documentsInBlob = await _documentStore.GetAllDocumentsAsync(_containerName);
            _logger.LogInformation("Successfully fetched {Count} documents", documentsInBlob?.Count() ?? 0);
            
            return documentsInBlob ?? Enumerable.Empty<DocsPerThread>();
        }

        public async Task<List<IndexDoc>> GetDocumentChunksAsync(string documentId)
        {
            if (string.IsNullOrEmpty(documentId))
            {
                throw new ArgumentNullException(nameof(documentId), "DocumentId is required");
            }

            _logger.LogInformation("Fetching chunks for documentId: {DocumentId}", documentId);
            var documents = await _searchService.GetDocumentAsync(documentId);
            _logger.LogInformation("Successfully fetched chunks for documentId: {DocumentId}", documentId);
            
            return documents;
        }

        public async Task<IEnumerable<Thread>> GetAllThreadsAsync()
        {
            _logger.LogInformation("Fetching all threads");
            var threads = await _threadRepository.GetAllThreads();
            _logger.LogInformation("Successfully fetched {Count} threads", threads?.Count ?? 0);
            
            return threads ?? Enumerable.Empty<Thread>();
        }

        public async Task<SystemHealthStatus> CheckSystemHealthAsync()
        {
            _logger.LogInformation("Performing system health check");
            
            var healthStatus = new SystemHealthStatus
            {
                Timestamp = DateTime.UtcNow,
                Components = new Dictionary<string, ComponentStatus>()
            };

            // Check AI Service
            await CheckAIServiceHealthAsync(healthStatus);

            // Check Thread Repository
            await CheckThreadRepositoryHealthAsync(healthStatus);

            // Check Search Service
            await CheckSearchServiceHealthAsync(healthStatus);

            // Check Document Registry
            await CheckDocumentRegistryHealthAsync(healthStatus);

            // Check Document Store
            await CheckDocumentStoreHealthAsync(healthStatus);

            // Calculate overall health status
            healthStatus.IsHealthy = healthStatus.Components.Values.All(c => c.IsWorking);
            
            _logger.LogInformation("System health check completed. System is {HealthStatus}",
                healthStatus.IsHealthy ? "healthy" : "unhealthy");
                
            return healthStatus;
        }

        #region Health Check Methods

        private async Task CheckAIServiceHealthAsync(SystemHealthStatus healthStatus)
        {
            try
            {
                var chatHistory = new ChatHistory("health check test", AuthorRole.User);
                var chatCompletion = await _aiService.GetChatCompletion(chatHistory, Enums.CompletionType.Chat);

                healthStatus.Components["AIService"] = new ComponentStatus
                {
                    IsWorking = chatCompletion != null && chatCompletion.IsSuccess,
                    Message = chatCompletion != null && chatCompletion.IsSuccess
                        ? "Successfully generated chat completion"
                        : "Failed to generate chat completion"
                };
                
                _logger.LogInformation("AI Service health check: {Status}", 
                    chatCompletion != null && chatCompletion.IsSuccess ? "Passed" : "Failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Service health check failed");
                healthStatus.Components["AIService"] = new ComponentStatus
                {
                    IsWorking = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        private async Task CheckThreadRepositoryHealthAsync(SystemHealthStatus healthStatus)
        {
            try
            {
                var threads = await _threadRepository.GetAllThreads();

                healthStatus.Components["ThreadRepository"] = new ComponentStatus
                {
                    IsWorking = threads != null,
                    Message = threads != null
                        ? $"Successfully retrieved {threads.Count} threads"
                        : "Failed to retrieve threads"
                };
                
                _logger.LogInformation("Thread Repository health check: {Status}", 
                    threads != null ? "Passed" : "Failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Thread Repository health check failed");
                healthStatus.Components["ThreadRepository"] = new ComponentStatus
                {
                    IsWorking = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        private async Task CheckSearchServiceHealthAsync(SystemHealthStatus healthStatus)
        {
            try
            {
                long documentsInIndex = await _searchService.GetSearchResultsCountAsync();

                healthStatus.Components["SearchService"] = new ComponentStatus
                {
                    IsWorking = true, // Service responded successfully
                    Message = $"Successfully retrieved {documentsInIndex} search results"
                };
                
                _logger.LogInformation("Search Service health check: Passed. Found {Count} documents", documentsInIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search Service health check failed");
                healthStatus.Components["SearchService"] = new ComponentStatus
                {
                    IsWorking = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        private async Task CheckDocumentRegistryHealthAsync(SystemHealthStatus healthStatus)
        {
            try
            {
                var threads = await _threadRepository.GetAllThreads();
                var threadId = threads?.FirstOrDefault()?.Id;

                bool hasDocuments = false;
                string message = "No threads available to check documents";
                int documentCount = 0;

                if (!string.IsNullOrEmpty(threadId))
                {
                    var documents = await _documentRegistry.GetDocsPerThreadAsync(threadId);
                    documentCount = documents?.Count ?? 0;
                    hasDocuments = documentCount > 0;
                    message = hasDocuments
                        ? $"Successfully retrieved {documentCount} documents from thread"
                        : "No documents found in thread";
                }

                healthStatus.Components["DocumentRegistry"] = new ComponentStatus
                {
                    IsWorking = threadId != null, // At least the service responded
                    Message = message
                };
                
                _logger.LogInformation("Document Registry health check: {Status}", 
                    threadId != null ? "Passed" : "Failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Document Registry health check failed");
                healthStatus.Components["DocumentRegistry"] = new ComponentStatus
                {
                    IsWorking = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        private async Task CheckDocumentStoreHealthAsync(SystemHealthStatus healthStatus)
        {
            try
            {
                var blobDocuments = await _documentStore.GetAllDocumentsAsync(_containerName);

                healthStatus.Components["DocumentStore"] = new ComponentStatus
                {
                    IsWorking = blobDocuments != null,
                    Message = blobDocuments != null
                        ? $"Successfully retrieved documents from blob storage"
                        : "Failed to retrieve documents from blob storage"
                };
                
                _logger.LogInformation("Document Store health check: {Status}", 
                    blobDocuments != null ? "Passed" : "Failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Document Store health check failed");
                healthStatus.Components["DocumentStore"] = new ComponentStatus
                {
                    IsWorking = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        #endregion
    }

    // Health check response models
    public class SystemHealthStatus
    {
        public bool IsHealthy { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, ComponentStatus> Components { get; set; } = new();
    }

    public class ComponentStatus
    {
        public bool IsWorking { get; set; }
        public string? Message { get; set; }
    }
}