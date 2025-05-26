using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Chat
{
    public class Settings
    {
        public bool AllowInitialPromptRewrite { get; set; }

        public bool AllowFollowUpPrompts { get; set; }

        public bool AllowInitialPromptToHelpUser { get; set; }
        public bool UseSemanticRanker {get;set;}

        public List<PredefinedPrompt>? PredefinedPrompts { get; set; }

        public double Temperature { get; set; }
        public int Seed { get; set; }
        public List<Tool>? Tools { get; set; }
    }

    public record PredefinedPrompt
    { 
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Prompt { get; set; }
    }

    public record Tool
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        //public required string ToolType { get; set; }
        //public required string ToolName { get; set; }
    }

    public class ThreadSafeSettings
    {
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
        private Settings _settings;

        public ThreadSafeSettings(Settings initialSettings)
        {
            _settings = initialSettings;
        }

        public Settings GetSettings()
        {
            _lock.EnterReadLock();
            try
            {
                // Return a copy to prevent external modification
                return new Settings
                {
                    AllowFollowUpPrompts = _settings.AllowFollowUpPrompts,
                    AllowInitialPromptRewrite = _settings.AllowInitialPromptRewrite,
                    UseSemanticRanker = _settings.UseSemanticRanker,
                    AllowInitialPromptToHelpUser = _settings.AllowInitialPromptToHelpUser,
                    PredefinedPrompts = _settings.PredefinedPrompts?.ToList(), // Create a new list
                    Temperature = _settings.Temperature,
                    Seed = _settings.Seed,
                    Tools = _settings.Tools
                };
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void UpdateSettings(Settings newSettings)
        {
            _lock.EnterWriteLock();
            try
            {
                _settings.AllowFollowUpPrompts = newSettings.AllowFollowUpPrompts;
                _settings.AllowInitialPromptRewrite = newSettings.AllowInitialPromptRewrite;
                _settings.UseSemanticRanker = newSettings.UseSemanticRanker;
                _settings.AllowInitialPromptToHelpUser = newSettings.AllowInitialPromptToHelpUser;
                _settings.PredefinedPrompts = newSettings.PredefinedPrompts?.ToList(); // Create a new list
                _settings.Temperature = newSettings.Temperature;
                _settings.Seed = newSettings.Seed;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
