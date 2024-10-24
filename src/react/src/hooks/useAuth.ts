import { useMsal } from "@azure/msal-react";
import { useState, useEffect } from "react";

export const useAuth = () => {

    const { instance } = useMsal();
    const [accessToken, setAccessToken] = useState<string>("");
    const userId = instance.getAllAccounts()[0].localAccountId;
    
    let accessTokenRequest = { 
        scopes: [import.meta.env.VITE_BACKEND_SCOPE],
        account: instance.getAllAccounts()[0]
    };

    useEffect(() => {
        const fetchData = async () => {
            try{
                const response = await instance.acquireTokenSilent(accessTokenRequest);
                setAccessToken(response.accessToken);
                console.log(response.accessToken)
            }catch(error){
                console.log("Silent token acquisition failed. Acquiring token using redirect.");
                console.error(error);
            }
        };
        fetchData();
    },[]);

    return { userId, accessToken }; 
    
}
