import { useNavigate } from "react-router-dom";
import useAuthStore from "@/store/authStore.ts";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button.tsx";
import { PageWrapper } from "@/components/zen/PageWrapper";

export default function Login() {
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
                navigate("/home");
                break;
            case "unauthenticated":
                break;
            case "pending":
                break;
            default:
                break;
        }
    }, [isLogged, navigate]);

    return (
        <PageWrapper>
            <div className="max-w-md mx-auto bg-parchment-50 border border-[rgba(60,50,40,0.10)] rounded-xl shadow-zen-sm p-6">
                <h1 className="font-serif text-2xl font-bold text-ink-400 mb-5">Login</h1>
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
                    <Button type="submit" variant="dark" size="full">
                        Login
                    </Button>
                </form>
            </div>
        </PageWrapper>
    );
}
