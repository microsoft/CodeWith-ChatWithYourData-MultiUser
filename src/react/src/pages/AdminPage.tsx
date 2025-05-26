import {
    Spinner,
    makeStyles,
    tokens,
    TabList,
    Tab
} from "@fluentui/react-components";
import { useState } from "react";
import { useAdminDocuments } from "../hooks/useAdminDocuments";
import { useSettings } from "../hooks/useSettings";
import { useHealthCheck } from "../hooks/useHealthCheck";
import { DocumentsTab } from "../components/admin/DocumentsTab";
import { DocumentChunksTab } from "../components/admin/DocumentChunksTab";
import { SettingsTab } from "../components/admin/SettingsTab";
import { PredefinedPromptsTab } from "../components/admin/PredefinedPromptsTab";
import { HealthTab } from "../components/admin/HealthTab";
import { LoggingSettingsTab } from "../components/admin/LoggingSettingsTab";
import { PredefinedPrompt } from "../models/Settings";
import { useLayoutStyles } from "../styles/sharedStyles";
import LoggingService from "../services/LoggingService";

const useStyles = makeStyles({
    container: {
        display: 'flex',
        flexDirection: 'column',
        marginLeft: tokens.spacingVerticalM,
        marginRight: tokens.spacingVerticalM,
        height: '100vh',
        overflow: 'hidden',
    },
    header: {
        display: 'flex',
        flexDirection: 'column',
        gap: '10px'
    },
    tabContent: {
        marginTop: tokens.spacingVerticalM,
        minWidth: '800px',
        flex: '1 1 auto',
        overflow: 'hidden',
    },
    tabContainer: {
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
        overflow: 'hidden',
    },
    contentScroll: {
        flex: '1 1 auto',
        padding: tokens.spacingVerticalXS,
    }
});

export const AdminPage = () => {
    const styles = useStyles();
    const layoutStyles = useLayoutStyles();
    const [selectedTab, setSelectedTab] = useState<string>("settings");
    
    const {
        allDocuments,
        documentChunks,
        isLoadingDocuments,
        isLoadingChunks,
        handleSearch,
        documentId,
        setDocumentId
    } = useAdminDocuments();

    //Settings state and handlers
    const { settings, updateSettings, isLoading: isLoadingSettings } = useSettings();
    
    // Health check data and handlers - don't load immediately
    const { healthData, isLoading: isLoadingHealth, error: healthError, refreshHealthCheck } = useHealthCheck(false);

    const handleSettingsUpdate = (newSettings: { 
        temperature: number; 
        seed: number;
        allowFollowUpPrompts: boolean;
        allowInitialPromptRewrite: boolean;
        allowInitialPromptToHelpUser: boolean;
        useSemanticRanker: boolean;
    }) => {
        if (settings) {
            const updatedSettings = {
                ...settings,
                predefinedPrompts: settings.predefinedPrompts,
                temperature: newSettings.temperature,
                seed: newSettings.seed,
                allowFollowUpPrompts: newSettings.allowFollowUpPrompts,
                allowInitialPromptRewrite: newSettings.allowInitialPromptRewrite,
                allowInitialPromptToHelpUser: newSettings.allowInitialPromptToHelpUser,                useSemanticRanker: newSettings.useSemanticRanker
            };
            LoggingService.log("Updating settings:", updatedSettings);
            updateSettings(updatedSettings);
        }
    };

    const handlePredefinePromptsUpdate = (prompts: PredefinedPrompt[]) => {
        // Only update predefinedPrompts, preserve all other settings
        if (settings) {
            const updatedSettings = {                ...settings,
                predefinedPrompts: prompts
            };
            LoggingService.log("Updating predefined prompts:", updatedSettings);
            updateSettings(updatedSettings);}
    };
    
    const handleTabSelect = (_event: React.SyntheticEvent, data: { value: unknown }) => {
        // Check if we have a valid value before updating state
        if (data && typeof data.value === 'string') {
            setSelectedTab(data.value);
            
            // Load health data when the health tab is selected
            if (data.value === "health" && !healthData && !isLoadingHealth) {
                refreshHealthCheck();
            }
        }
    };

    if (isLoadingDocuments && selectedTab === "documents") {
        return <Spinner />;
    }

    return (
        <div className={`${styles.container} ${layoutStyles.scrollContainer}`}>
            <div className={styles.header}>
                <h1>Admin Dashboard</h1>
                  <TabList selectedValue={selectedTab} onTabSelect={handleTabSelect}>
                    <Tab value="settings">Settings</Tab>
                    <Tab value="documents">Documents</Tab>
                    <Tab value="chunks">Document Chunks</Tab>
                    <Tab value="prompts">Predefined Prompts</Tab>
                    <Tab value="health">System Health</Tab>
                    <Tab value="logging">Logging</Tab>
                </TabList>
            </div>

            <div className={styles.tabContent}>
                {selectedTab === "documents" && (
                    <DocumentsTab 
                        documents={allDocuments} 
                        isLoading={isLoadingDocuments} 
                    />
                )}

                {selectedTab === "chunks" && (
                    <DocumentChunksTab
                        documentId={documentId}
                        setDocumentId={setDocumentId}
                        documentChunks={documentChunks}
                        isLoading={isLoadingChunks}
                        onSearch={handleSearch}
                    />
                )}

                {selectedTab === "settings" && (
                    <SettingsTab 
                        isLoading={isLoadingSettings || false}
                        settings={settings}
                        onSaveSettings={handleSettingsUpdate}
                    />
                )}

                {selectedTab === "prompts" && (
                    <PredefinedPromptsTab
                        isLoading={isLoadingSettings || false}
                        prompts={settings?.predefinedPrompts || []}
                        onSavePrompts={handlePredefinePromptsUpdate}
                    />
                )}
                  {selectedTab === "health" && (
                    <HealthTab
                        healthData={healthData}
                        isLoading={isLoadingHealth}
                        error={healthError}
                        onRefresh={refreshHealthCheck}
                    />
                )}

                {selectedTab === "logging" && (
                    <LoggingSettingsTab />
                )}
            </div>
        </div>
    );
};