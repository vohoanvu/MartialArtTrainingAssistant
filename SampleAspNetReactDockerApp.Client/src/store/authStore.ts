﻿import { User } from "@/types/global";
import { create } from "zustand";
//setAuthToken, setRefreshToken 
import { removeAuthToken, removeRefreshToken } from "@/lib/utils.ts";
import { paths } from "@/services/endpoints.ts";
import { createJSONStorage, persist } from "zustand/middleware";
//import { t } from "i18next";

export interface RegisterFighterBody {
    email: string;
    password: string;
    fighterRole: string;
    fighterName: string;
    height: number;
    weight: number;
    bmi: number;
    gender: string;
    birthdate: string;
    beltColor: string;
    maxWorkoutDuration: number;
    experience: number;
}


/**
 * @AuthState is an interface that defines the state of the AuthStore.
 * @accessToken: The access token used to authenticate requests to the server.
 * @refreshToken: The refresh token used to get a new access token when the current one expires.
 * @user: The user object that represents the currently authenticated user.
 * @loginStatus: The status of the login process. Can be 'authenticated', 'unauthenticated', or 'pending'.
 *             'authenticated' means the user is authenticated and can access protected resources.
 *             'unauthenticated' means the user is not authenticated and cannot access protected resources.
 *             'pending' means the login process is in progress.
 */
interface AuthState {
    accessToken: string | null;
    refreshToken: string | null;
    user: User | null;
    loginStatus: 'authenticated' | 'unauthenticated' | 'pending';
}

/**
 * AuthActions is an interface that defines the actions that can be performed on the AuthStore.
 * @setTokens: A function that sets the access token and refresh token in the store.
 * @clearTokens: A function that clears the access token and refresh token from the store.
 * @setUser: A function that sets the user object in the store.
 * @clearUser: A function that clears the user object from the store.
 * @setLoginStatus: A function that sets the login status in the store.
 * @login: A function that performs the login process using the given username and password.
 * @signIn: A function that performs the sign in process using the given token.
 * @logout: A function that logs out the user by clearing the tokens and user object from the store.
 * @hydrate: A function that hydrates the store by checking if the user is already authenticated and setting the login status accordingly.
 *         This is useful for persisting the authentication state across page refreshes.
 *         It should be called once when the app is initialized to check if the user is already authenticated.
 */
interface AuthActions {
    setTokens: (accessToken: string, refreshToken: string) => void;
    clearTokens: () => void;
    setUser: (user: User) => void;
    clearUser: () => void;
    setLoginStatus: (status: 'authenticated' | 'unauthenticated' | 'pending') => void;
    login: (request: paths["/api/fighter/login"]["post"]["requestBody"]["content"]["application/json"]) => Promise<{ successful: boolean, response: string | null }>;
    signIn: () => Promise<void>;
    register: (email: string, password: string) => Promise<{ successful: boolean, response: string | null }>;
    logout: () => void;
    hydrate: () => Promise<void>;
    getUserInfo: () => Promise<void>;
    registerFighter: (body: RegisterFighterBody) => Promise<{ successful: boolean, response: string | null }>;
}

/**
 * AuthStore is a type that represents the AuthStore.
 * It is a combination of AuthState and AuthActions.
 */
type AuthStore = AuthState & AuthActions;

/**
 * useAuthStore is a hook that creates and returns the AuthStore.
 * It uses the create function from zustand to create the store.
 */
const useAuthStore = create<AuthStore>()(
    persist(
        (set, get) => ({
            accessToken: null,
            refreshToken: null,
            user: null,
            loginStatus: 'unauthenticated',
            setTokens: (accessToken, refreshToken) => set({ accessToken, refreshToken }),
            clearTokens: () => set({ accessToken: null, refreshToken: null }),
            setUser: (user) => set({ user }),
            clearUser: () => set({ user: null }),
            setLoginStatus: (status) => set({ loginStatus: status }),
            login: async (request: paths["/api/fighter/login"]["post"]["requestBody"]["content"]["application/json"]) => {

                const response = await fetch("api/fighter/login", {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(request)
                });

                if (response.ok) {
                    const responseData: paths["/api/fighter/login"]["post"]["responses"]["200"]["content"]["application/json"] = await response.json();
                    get().setTokens(responseData.accessToken || "", responseData.refreshToken || "");
                    get().setLoginStatus('authenticated');
                    get().getUserInfo();
                    return { successful: true, response: null };
                } else {
                    get().setLoginStatus('unauthenticated');
                    return { successful: false, response: await response.text() };
                }
            },
            signIn: async () => {
                const response = await fetch('/api/auth/v1/login', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    }
                });

                if (response.ok) {
                    const { accessToken, refreshToken } = await response.json();
                    get().setTokens(accessToken, refreshToken);
                    get().setLoginStatus('authenticated');
                } else {
                    const refreshToken = get().refreshToken;
                    if (refreshToken === null) {
                        get().setLoginStatus('unauthenticated');
                        window.location.href = "/login";
                        return;
                    }

                    const response = await fetch('/api/auth/v1/refresh', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Authorization': `Bearer ${refreshToken}`
                        }
                    });

                    if (response.ok) {
                        const { accessToken, refreshToken } = await response.json();
                        get().setTokens(accessToken, refreshToken);
                        get().setLoginStatus('authenticated');
                    } else {
                        get().setLoginStatus('unauthenticated');
                        window.location.href = "/login";
                    }
                }
            },
            logout: () => {
                get().clearTokens();
                get().clearUser();
                removeAuthToken();
                removeRefreshToken();
                get().setLoginStatus('unauthenticated');
                //Redirect back /Home page
                window.location.href = "/home";
            },
            hydrate: async () => {
                const { accessToken, refreshToken, setLoginStatus, setTokens, getUserInfo } = get();
                if (!refreshToken || !accessToken) {
                    setLoginStatus('unauthenticated');
                    window.location.href = '/login';
                    return;
                }
                try {
                    const response = await fetch('/api/auth/v1/refresh', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ accessToken, refreshToken }),
                        credentials: 'include', // Include cookies (e.g., Identity cookie)
                    });
                    console.log('POST /api/auth/v1/refresh...', response.status, response.ok);
                    if (response.ok) {
                        const { accessToken: newAccessToken, refreshToken: newRefreshToken } = await response.json();
                        setTokens(newAccessToken, newRefreshToken);
                        setLoginStatus('authenticated');
                        await getUserInfo();
                    } else {
                        console.error('Refresh failed:', response.status, await response.text());
                        setLoginStatus('unauthenticated');
                        window.location.href = '/login';
                    }
                } catch (error) {
                    console.error('Refresh error:', error);
                    setLoginStatus('unauthenticated');
                    window.location.href = '/login';
                }
            },
            register: async (email, password) => {
                const response = await fetch("api/auth/v1/register", {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ email, password })
                });

                const responseCode = response.status;
                return responseCode === 200 ? { successful: true, response: null } : { successful: false, response: await response.text() };
            },
            registerFighter: async (body: RegisterFighterBody) => {
                const response = await fetch("api/fighter/register", {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(body)
                });
                console.log('POST /api/fighter/register...', response);
                const responseCode = response.status;
                return responseCode === 201 ?
                    { successful: true, response: null } :
                    { successful: false, response: await response.text() };
            },
            getUserInfo: async () => {
                const accessToken = get().accessToken;
                if (!accessToken) {
                    console.error("Access token is not available.");
                }

                const response = await fetch("/api/auth/v1/manage/info", {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json',
                        //'Authorization': `Bearer ${accessToken}`
                    }
                });
                if (response.ok) {
                    const userData = await response.json();
                    console.log('GET /api/auth/v1/manage/info...', userData);
                    const { userInfo, fighterInfo } = userData;
                    const user = userInfo as User;
                    user.fighterInfo = fighterInfo;
                    get().setUser(user);
                } else {
                    console.error("Failed to fetch user info:", await response.text());
                    window.location.href = "/login";
                }
            },
        }),
        {
            name: 'auth-storage',
            storage: createJSONStorage(() => sessionStorage)
        }
    )
);

export default useAuthStore;
