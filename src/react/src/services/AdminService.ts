// filepath: d:\repos\codewith\codewith-philips\CodeWith-ChatWithYourData-MultiUser\src\react\src\services\AdminService.ts
import { IDocument } from "../models/Document";
import { IIndexDoc } from "../models/IndexDoc";
import { env } from "../config/env";
import { ISetting } from "../models/Settings";
import LoggingService from "./LoggingService";

// Add new HealthCheck interface
export interface HealthCheck {
    isHealthy: boolean;
    timestamp: string;
    components: {
        [key: string]: {
            isWorking: boolean;
            message: string;
        }
    }
}

export class AdminService {
    private readonly baseUrl = env.BACKEND_URL;

    // Get all documents across all threads
    public getAllDocumentsAsync = async (token: string): Promise<IDocument[]> => {
        try {
            const response = await fetch(`${this.baseUrl}/admin/documents`, {
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            });
            if (!response.ok) {
                throw new Error(`Error fetching documents: ${response.statusText}`);
            }
            const documents: IDocument[] = await response.json();
            return documents;
        } catch (error) {
            console.error('Failed to fetch documents:', error);
            throw error;
        }
    };

    // Search for document chunks by documentId
    public searchDocumentChunksAsync = async ({documentId, token}: {documentId: string, token: string}): Promise<IIndexDoc[]> => {
        try {
            const response = await fetch(`${this.baseUrl}/admin/documents/${documentId}/chunks`, {
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            });
            if (!response.ok) {
                throw new Error(`Error searching document chunks: ${response.statusText}`);
            }
            const chunks: IIndexDoc[] = await response.json();
            return chunks;
        } catch (error) {
            console.error('Failed to search document chunks:', error);
            throw error;
        }
    };

    // Get Settings
    public getSettingsAsync = async (token: string): Promise<ISetting> => {
        try {
            const response = await fetch(`${this.baseUrl}/admin/settings`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json',
                }
            });
            if (!response.ok) {
                throw new Error(`Error fetching settings: ${response.statusText}`);
            }            const settings: ISetting = await response.json();
            // Log settings for debugging
            LoggingService.log("Fetched settings:", settings);
            return settings;
        } catch (error) {
            console.error('Failed to fetch settings:', error);
            throw error;
        }
    }

    // Update Settings
    public updateSettingsAsync = async (settings: ISetting, token: string): Promise<ISetting> => {
        try {
            const response = await fetch(`${this.baseUrl}/admin/settings`, {
                method: 'PATCH',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(settings)
            });
            if (!response.ok) {
                throw new Error(`Error updating settings: ${response.statusText}`);
            }
            const updatedSettings: ISetting = await response.json();
            return updatedSettings;
        } catch (error) {
            console.error('Failed to update settings:', error);
            throw error;
        }
    }

    // Get health status of backend services
    public getHealthCheckAsync = async (token: string): Promise<HealthCheck> => {
        try {
            const response = await fetch(`${this.baseUrl}/admin/check`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json',
                }
            });
            if (!response.ok) {
                throw new Error(`Error fetching health check: ${response.statusText}`);
            }            const healthData: HealthCheck = await response.json();
            LoggingService.log("Health check data:", healthData);
            return healthData;
        } catch (error) {
            console.error('Failed to fetch health check:', error);
            throw error;
        }
    }
}