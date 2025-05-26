import { useMsal } from "@azure/msal-react";
import { useState, useEffect, useCallback, useRef } from "react";
import { env } from "../config/env";

// Function to decode JWT token and extract expiration time
const getTokenExpirationTime = (token: string): number => {
    try {
        const payloadBase64 = token.split('.')[1];
        const payload = JSON.parse(atob(payloadBase64));
        return payload.exp * 1000; // Convert to milliseconds
    } catch (error) {
        console.error('Error decoding token:', error);
        return 0;
    }
};

export const useAuth = () => {
    const { instance } = useMsal();
    const [accessToken, setAccessToken] = useState<string>("");
    const userId = instance.getAllAccounts()[0]?.localAccountId || "";
    const refreshTimerRef = useRef<NodeJS.Timeout | null>(null);
    
    // Define the token request
    const getAccessTokenRequest = () => ({
        scopes: [env.BACKEND_SCOPE],
        account: instance.getAllAccounts()[0]
    });

    // Clear any existing refresh timer
    const clearRefreshTimer = () => {
        if (refreshTimerRef.current) {
            clearTimeout(refreshTimerRef.current);
            refreshTimerRef.current = null;
        }
    };

    // Acquire a token with silent or interactive fallback
    const acquireToken = useCallback(async () => {
        const accessTokenRequest = getAccessTokenRequest();
        if (!accessTokenRequest.account) {
            console.error('No account found. User may not be authenticated.');
            return;
        }

        try {
            // Attempt silent token acquisition
            const response = await instance.acquireTokenSilent(accessTokenRequest);
            setAccessToken(response.accessToken);

            // Set up token refresh before expiration
            const expirationTime = getTokenExpirationTime(response.accessToken);
            const currentTime = Date.now();
            const timeToExpiry = expirationTime - currentTime;
            
            // Refresh token when it's at 85% of its lifetime
            const refreshTime = Math.max(timeToExpiry * 0.85, 0);
            
            clearRefreshTimer();
            
            if (refreshTime > 0) {
                refreshTimerRef.current = setTimeout(() => {
                    console.log('Token approaching expiration, refreshing...');
                    acquireToken();
                }, refreshTime);
            }
            
            console.log(`Token will refresh in ${Math.round(refreshTime / 1000)} seconds`);
            
        } catch (error) {
            console.error('Silent token acquisition failed. Attempting interactive acquisition.', error);
            // Try interactive token acquisition as fallback
            try {
                const response = await instance.acquireTokenPopup(accessTokenRequest);
                setAccessToken(response.accessToken);
                
                // Also set up refresh for tokens acquired interactively
                const expirationTime = getTokenExpirationTime(response.accessToken);
                const currentTime = Date.now();
                const timeToExpiry = expirationTime - currentTime;
                const refreshTime = Math.max(timeToExpiry * 0.85, 0);
                
                clearRefreshTimer();
                
                if (refreshTime > 0) {
                    refreshTimerRef.current = setTimeout(() => {
                        console.log('Token approaching expiration, refreshing...');
                        acquireToken();
                    }, refreshTime);
                }
            } catch (interactiveError) {
                console.error('Interactive token acquisition failed.', interactiveError);
            }
        }
    }, [instance]);

    useEffect(() => {
        // Initial token acquisition
        acquireToken();
        
        // Clean up on unmount
        return () => {
            clearRefreshTimer();
        };
    }, [instance, acquireToken]);    // Expose a method to manually refresh the token if needed
    const refreshToken = useCallback(() => {
        console.log('Manual token refresh requested');
        acquireToken();
    }, [acquireToken]);
    
    return { 
        userId, 
        accessToken,
        refreshToken,
        isAuthenticated: !!accessToken
    };
};
