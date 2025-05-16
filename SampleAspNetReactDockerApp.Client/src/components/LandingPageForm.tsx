import { Link, useNavigate } from 'react-router-dom';

import useAuthStore from "@/store/authStore";
import { useEffect, useState } from "react";
import { Button } from './ui/button';

const LandingPageForm = () => {
    const navigate = useNavigate();

    const login = useAuthStore((state) => state.login);
    const isLogged = useAuthStore((state) => state.loginStatus);
    const [errorMessage, setErrorMessage] = useState('');

    const loginActionForm = async (email: string, password: string) => {
        try {
            const resp = await login({ email, password });
            if (resp.successful) {
                navigate("/class-session");
            } else {
                setErrorMessage("Login failed, reason: " + resp.response);
                console.log("Login failed: ", resp.response)
            }
        } catch (error) {
            window.alert("Error: " + error);
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
        // Use the proxied API path so Vite forwards to the backend
        window.location.href = `https://localhost:7191/api/externalauth/signin-${provider}?returnUrl=${encodeURIComponent(window.location.origin + "/sso-callback")}`;
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
                        className="w-full">
                        Login
                    </Button>
                </form>
                <div className="flex flex-col gap-2 mt-6">
                    <Button
                        type="button"
                        variant="outline"
                        className="w-full h-full p-0"
                        onClick={() => handleSSOLogin("google")}
                    >
                        <img
                            src="/google/neutral/web_neutral_sq_na.svg"
                            alt="Google"
                            className="h-auto w-auto mr-4"
                        />
                        Sign in with Google
                    </Button>
                </div>
            </div>

            <p className="mt-4">
                New User?{' '}
                <Link to="/register" className="text-blue-500 hover:underline">
                    Get started by filling out our registration form here!
                </Link>
            </p>
            <div className="flex flex-col items-center mt-8 space-y-4">
                <div className="text-center">
                    <h2 className="text-2xl font-bold">For Students</h2>
                    <p>Access training materials, track progress, and join sessions to get paired up with your ideal partner.</p>
                </div>
                <div className="text-center">
                    <h2 className="text-2xl font-bold">For Instructors</h2>
                    <p>Create and manage training sessions, track student progress, provide feedback, and access to AI Curriculum assistant.</p>
                </div>
            </div>
        </div>
    );
};

export default LandingPageForm;