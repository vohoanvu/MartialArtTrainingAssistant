import { useEffect, useState } from 'react';
import { getAiConsoleAccess } from '@/services/api';

/**
 * Asks the server whether the signed-in user may use the AI Console (admin = server-side email
 * allow-list; there is no role in the JWT). Resolves to false on any error/401/403 and whenever
 * there is no token. Called once per token.
 */
export function useAiConsoleAccess(accessToken: string | null | undefined): boolean {
    const [isAdmin, setIsAdmin] = useState(false);

    useEffect(() => {
        if (!accessToken) {
            setIsAdmin(false);
            return;
        }
        let cancelled = false;
        getAiConsoleAccess({ jwtToken: accessToken }).then((result) => {
            if (!cancelled) setIsAdmin(result);
        });
        return () => {
            cancelled = true;
        };
    }, [accessToken]);

    return isAdmin;
}
