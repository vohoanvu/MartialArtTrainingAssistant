import { Link, useNavigate } from 'react-router-dom';

import useAuthStore from "@/store/authStore";
import { useEffect, useState } from "react";
import { Button } from './ui/button';

const LandingPageForm = () => {
    const navigate = useNavigate();

    const login = useAuthStore((state) => state.login);
    const isLogged = useAuthStore((state) => state.loginStatus);
    const [errorMessage, setErrorMessage] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [isSSOloading, setIsSSOloading] = useState(false);

    const loginActionForm = async (email: string, password: string) => {
        try {
            setIsLoading(true);
            const resp = await login({ email, password });
            if (resp.successful) {
                navigate("/class-session");
            } else {
                setErrorMessage("Login failed, reason: " + resp.response);
                console.log("Login failed: ", resp.response)
            }
        } catch (error) {
            window.alert("Error: " + error);
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        switch (isLogged) {
            case "authenticated":
                navigate("/class-session"); //go to ClassSession after login
                break;
            case "unauthenticated":
                navigate("/home");
                break;
            case "pending":
                break;
            default:
                break;
        }

    }, [isLogged, navigate]);

    const handleSSOLogin = (provider: "google" | "facebook") => {
        setIsSSOloading(true);
        // Use a relative path so Nginx proxies /api to the backend in production
        window.location.href = `/api/externalauth/signin-${provider}?returnUrl=${encodeURIComponent(window.location.origin + "/sso-callback")}`;
    };

    return (
        <div className="flex flex-col items-center justify-center max-h-screen">
            <h1 className="text-4xl font-bold mb-4">Train like a warrior</h1>
            <div className="container mx-auto max-w-md p-8 shadow-lg rounded-lg">
                <h1 className="text-3xl font-bold text-primary mb-6">Login</h1>
                <form
                    className="space-y-4"
                    onSubmit={async (e) => {
                        e.preventDefault();
                        const formData = new FormData(e.currentTarget);
                        const email = formData.get("email") as string;
                        const password = formData.get("password") as string;
                        await loginActionForm(email, password);
                    }}
                >
                    {errorMessage &&
                        <>
                            <p className="text-red-500 line-clamp-5">{errorMessage}</p>
                            <p className="text-red-300 text-xl font-bold">Check console for details</p>
                        </>
                    }
                    <div>
                        <label htmlFor="email" className="block text-sm font-medium">
                            Email
                        </label>
                        <input
                            type="email"
                            id="email"
                            name="email"
                            className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
                            onChange={() => {
                                if (errorMessage !== '')
                                    setErrorMessage('');
                            }}
                            required
                        />
                    </div>
                    <div>
                        <label htmlFor="password" className="block text-sm font-medium">
                            Password
                        </label>
                        <input
                            type="password"
                            id="password"
                            name="password"
                            className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
                            onChange={() => {
                                if (errorMessage !== '')
                                    setErrorMessage('');
                            }}
                            required
                        />
                    </div>
                    <Button type="submit"
                        variant={"outline"}
                        className="w-full"
                        disabled={isLoading}
                    >
                        {isLoading ? "Logging in..." : "Login"}
                    </Button>
                </form>
                <div className="flex flex-col gap-2 mt-6">
                    <Button
                        type="button"
                        variant="outline"
                        className="w-full h-full p-0 relative overflow-hidden"
                        onClick={() => handleSSOLogin("google")}
                        disabled={isSSOloading}
                    >
                        <div className="flex items-center justify-center relative z-10">
                            <img
                                src="/signin-assets/Web/svg/light/web_light_sq_na.svg"
                                alt="Google"
                                className="h-auto w-auto mr-4 dark:hidden"
                            />
                            <img
                                src="/signin-assets/Web/svg/dark/web_dark_sq_na.svg"
                                alt="Google"
                                className="h-auto w-auto mr-4 hidden dark:block"
                            />
                            {isSSOloading ? "Signing in..." : "Sign in with Google"}
                        </div>
                        {isSSOloading && (
                            <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/30 to-transparent pointer-events-none animate-shimmer" />
                        )}
                    </Button>
                </div>
            </div>

            <p className="mt-4">
                Want to start quickly?{' '}
                {/* <Link to="/register" className="text-blue-500 hover:underline">
                    Get started by either filling out our registration form or sign in with Google!
                </Link> */}
                <Link to="/register" className="text-blue-500 hover:underline">
                    Sign in with Google to start using our premium Instructor features!
                </Link>
            </p>
            <div className="flex flex-col items-center mt-8 space-y-4">
                <div className="text-center">
                    <h2 className="text-2xl font-bold">For Students</h2>
                    <p>Upload training videos and access AI-driven feedback, join class sessions to get paired up with your ideal partner.</p>
                </div>
                <div className="text-center">
                    <h2 className="text-2xl font-bold">For Instructors</h2>
                    <p>Create and manage training lessons, review Students training footage to provide feedback, and access to AI agent that organizes your class lessons.</p>
                </div>
            </div>
        </div>
    );
};

export default LandingPageForm;