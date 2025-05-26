// filepath: d:\repos\codewith\codewith-philips\CodeWith-ChatWithYourData-MultiUser\src\react\src\hooks\useHealthCheck.ts
import { useEffect, useState } from "react";
import { AdminService, HealthCheck } from "../services/AdminService";
import { useAuth } from "./useAuth";

export function useHealthCheck(immediate = false) {
    const [healthData, setHealthData] = useState<HealthCheck | null>(null);
    const [isLoading, setIsLoading] = useState(immediate);
    const [error, setError] = useState<Error | null>(null);
    const { accessToken } = useAuth();
    const adminService = new AdminService();

    const fetchHealthCheck = async () => {
        if (!accessToken) return;
        
        try {
            setIsLoading(true);
            setError(null);
            const data = await adminService.getHealthCheckAsync(accessToken);
            setHealthData(data);
        } catch (err) {
            setError(err instanceof Error ? err : new Error('Failed to fetch health check data'));
            console.error("Error fetching health check:", err);
        } finally {
            setIsLoading(false);
        }
    };

    // Only fetch data initially if immediate flag is true
    useEffect(() => {
        if (immediate) {
            fetchHealthCheck();
        }
    }, [accessToken, immediate]);

    return {
        healthData,
        isLoading,
        error,
        refreshHealthCheck: fetchHealthCheck
    };
}