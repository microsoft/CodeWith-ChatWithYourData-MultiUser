import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AdminService } from "../services/AdminService";
import { useAuth } from "./useAuth";
import { ISetting, PredefinedPrompt } from "../models/Settings";
import LoggingService from "../services/LoggingService";

export const useSettings = () => {
    const { accessToken } = useAuth();
    const adminService = new AdminService();
    const queryClient = useQueryClient();

    const { data: settings, isLoading, error } = useQuery({
        queryKey: ['system-settings'],
        queryFn: () => adminService.getSettingsAsync(accessToken),
        enabled: !!accessToken,
        staleTime: 0, // Always refresh when accessing settings
    });

    const { mutate: updateSettings } = useMutation({
        mutationFn: (newSettings: ISetting) => {
            // Get the current settings from the cache
            const currentSettings = queryClient.getQueryData(['system-settings']) as ISetting || {};

            // Create a complete settings object with default values for required fields
            const mergedSettings: ISetting = {
                allowInitialPromptToHelpUser: newSettings.allowInitialPromptToHelpUser ?? currentSettings.allowInitialPromptToHelpUser ?? false,
                allowInitialPromptRewrite: newSettings.allowInitialPromptRewrite ?? currentSettings.allowInitialPromptRewrite ?? false,
                allowFollowUpPrompts: newSettings.allowFollowUpPrompts ?? currentSettings.allowFollowUpPrompts ?? false,
                useSemanticRanker: newSettings.useSemanticRanker ?? currentSettings.useSemanticRanker ?? false,
                temperature: newSettings.temperature ?? currentSettings.temperature ?? 0.7,
                maxTokens: newSettings.maxTokens ?? currentSettings.maxTokens ?? 100,
                seed: newSettings.seed ?? currentSettings.seed ?? 42,                
                predefinedPrompts: newSettings.predefinedPrompts ?? currentSettings.predefinedPrompts ?? [],
                tools: newSettings.tools ?? currentSettings.tools ?? []
            };
            
            LoggingService.log("Updating settings - merged result:", mergedSettings);
            return adminService.updateSettingsAsync(mergedSettings, accessToken);
        },
        onSuccess: (data) => {
            LoggingService.log("Settings update successful:", data);
            // Optimistically update the cache with the new data
            queryClient.setQueryData(['system-settings'], data);
            // Then invalidate to ensure we're in sync with server
            queryClient.invalidateQueries({ queryKey: ['system-settings'] });
        },
        onError: (error) => {
            console.error("Error updating settings:", error);
            // Invalidate cache on error to ensure we get fresh data
            queryClient.invalidateQueries({ queryKey: ['system-settings'] });
        }
    });

    return {
        settings,
        updateSettings,
        isLoading,
        error
    };
};