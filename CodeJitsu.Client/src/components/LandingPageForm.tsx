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
                navigate("/class-session");
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
        window.location.href = `/api/externalauth/signin-${provider}?returnUrl=${encodeURIComponent(window.location.origin + "/sso-callback")}`;
    };

    return (
        <div className="flex flex-col items-center justify-center">
            <h2 className="font-serif text-2xl font-bold text-ink-400 mb-5">Train like a warrior</h2>
            <div className="w-full max-w-md bg-parchment-50 border border-[rgba(60,50,40,0.10)] rounded-xl shadow-zen-sm p-6">
                <h3 className="font-serif text-xl font-semibold text-ink-400 mb-4">Login</h3>
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
                    {errorMessage && (
                        <div className="bg-blood-100 border border-blood-200 rounded-md p-3">
                            <p className="font-sans text-sm text-blood-400 line-clamp-5">{errorMessage}</p>
                            <p className="font-sans text-xs text-blood-300 mt-1">Check console for details</p>
                        </div>
                    )}
                    <div>
                        <label htmlFor="email" className="font-display text-label font-semibold uppercase tracking-[0.1em] text-slate-zen400 mb-1.5 block">
                            Email
                        </label>
                        <input
                            type="email"
                            id="email"
                            name="email"
                            className="w-full bg-parchment-50 border-[1.5px] border-[rgba(60,50,40,0.18)] rounded-md px-3.5 py-2.5 font-sans text-sm text-ink-400 placeholder:text-slate-zen300 outline-none transition-all duration-fast ease-zen focus:border-samurai-400 focus:ring-2 focus:ring-samurai-400/10"
                            onChange={() => { if (errorMessage !== '') setErrorMessage(''); }}
                            required
                        />
                    </div>
                    <div>
                        <label htmlFor="password" className="font-display text-label font-semibold uppercase tracking-[0.1em] text-slate-zen400 mb-1.5 block">
                            Password
                        </label>
                        <input
                            type="password"
                            id="password"
                            name="password"
                            className="w-full bg-parchment-50 border-[1.5px] border-[rgba(60,50,40,0.18)] rounded-md px-3.5 py-2.5 font-sans text-sm text-ink-400 placeholder:text-slate-zen300 outline-none transition-all duration-fast ease-zen focus:border-samurai-400 focus:ring-2 focus:ring-samurai-400/10"
                            onChange={() => { if (errorMessage !== '') setErrorMessage(''); }}
                            required
                        />
                    </div>
                    <Button type="submit" variant="dark" size="full" disabled={isLoading}>
                        {isLoading ? "Logging in..." : "Login"}
                    </Button>
                </form>
                <div className="flex flex-col items-center gap-2 mt-5">
                    <button
                        type="button"
                        className="relative cursor-pointer bg-transparent border-none p-0 rounded-full overflow-hidden hover:opacity-90 transition-opacity duration-fast ease-zen disabled:opacity-50 disabled:cursor-not-allowed"
                        onClick={() => handleSSOLogin("google")}
                        disabled={isSSOloading}
                    >
                        <img
                            src="/signin-assets/Web/svg/light/web_light_rd_SI.svg"
                            alt="Sign in with Google"
                            className="h-11 w-auto"
                        />
                        {isSSOloading && (
                            <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/30 to-transparent pointer-events-none animate-shimmer rounded-full" />
                        )}
                    </button>
                </div>
            </div>

            <p className="font-sans text-sm text-slate-zen400 mt-5">
                Want to start quickly?{' '}
                <Link to="/register" className="text-samurai-400 hover:text-samurai-500 underline transition-colors duration-fast ease-zen">
                    Sign in with Google to start using our premium Instructor features!
                </Link>
            </p>
            <div className="flex flex-col items-center mt-7 space-y-4 max-w-lg">
                <div className="text-center">
                    <h2 className="font-serif text-xl font-bold text-ink-400">For Students</h2>
                    <p className="font-sans text-sm text-slate-zen400 mt-1">Upload training videos and access AI-driven feedback, join class sessions to get paired up with your ideal partner.</p>
                </div>
                <div className="text-center">
                    <h2 className="font-serif text-xl font-bold text-ink-400">For Instructors</h2>
                    <p className="font-sans text-sm text-slate-zen400 mt-1">Create and manage training lessons, review Students training footage to provide feedback, and access to AI agent that organizes your class lessons.</p>
                </div>
            </div>
        </div>
    );
};

export default LandingPageForm;
