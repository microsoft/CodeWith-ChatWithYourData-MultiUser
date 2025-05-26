/**
 * Type-safe environment variables configuration
 * This ensures that all required environment variables are present and correctly typed
 */

import LoggingService from "../services/LoggingService";

// Define the expected shape of our environment variables
interface EnvVariables {
  BACKEND_URL: string;
  BACKEND_SCOPE: string;
  PUBLIC_APP_ID: string;
  PUBLIC_AUTHORITY_URL: string;
}

// Get variables from process.env with proper error handling
function getEnvVariable(key: string): string {
  const value = import.meta.env[`VITE_${key}`] || process.env[`VITE_${key}`];
  
  if (!value) {
    console.error(`Missing environment variable: VITE_${key}`);
    return '';
  }
  
  return value as string;
}

// Create and export our config object with typed properties
export const env: EnvVariables = {
  BACKEND_URL: getEnvVariable('BACKEND_URL'),
  BACKEND_SCOPE: getEnvVariable('BACKEND_SCOPE'),
  PUBLIC_APP_ID: getEnvVariable('PUBLIC_APP_ID'),
  PUBLIC_AUTHORITY_URL: getEnvVariable('PUBLIC_AUTHORITY_URL'),
};

// Validate that all required env vars are present at startup
function validateEnv(): boolean {
  let isValid = true;
  
  Object.entries(env).forEach(([key, value]) => {
    if (!value) {
      console.error(`Environment variable VITE_${key} is required but not set`);
      isValid = false;
    }
  });
  
  return isValid;
}

// Run validation and log result
const isEnvValid = validateEnv();
if (!isEnvValid) {
  LoggingService.warn('Application may not function correctly due to missing environment variables');
} else {
  LoggingService.log('Environment variables validated successfully');
}
