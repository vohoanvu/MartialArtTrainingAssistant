import { ReactElement } from "react";
import { cn } from "@/lib/utils.ts";
import { useTranslation } from "react-i18next";
import { Link, useLocation } from "react-router-dom";
import useAuthStore from "@/store/authStore.ts";

export default function Navbar(): ReactElement {
    const { t } = useTranslation();
    const authStatus = useAuthStore((state) => state.loginStatus);
    const logout = useAuthStore((state) => state.logout);
    const authUser = useAuthStore((state) => state.user);
    const location = useLocation();

    const isActive = (path: string) => location.pathname === path;

    const linkClass = (path: string) =>
        cn(
            'font-sans text-sm transition-colors duration-fast ease-zen',
            isActive(path)
                ? 'text-parchment-100 border-b border-samurai-300 pb-0.5'
                : 'text-parchment-300 hover:text-parchment-100'
        );

    return (
        <nav className="bg-slate-zen700 border-b border-white/5 sticky top-0 z-50">
            <div className="max-w-[1120px] mx-auto px-5 flex items-center justify-between h-14">
                {/* Brand */}
                <Link to="/" className="flex items-center gap-2">
                    <img src="/codejitsu-favicon.png" alt="CodeJitsu" className="w-7 h-7" />
                    <span className="font-display text-base font-semibold text-parchment-100 tracking-wider">
                        CodeJitsu
                    </span>
                </Link>

                {/* Links */}
                <div className="flex items-center gap-5">
                    {authStatus === "authenticated" ? (
                        <>
                            <Link to="/video-analysis" className={linkClass('/video-analysis')}>
                                Video Analysis
                            </Link>
                            <Link to="/class-session" className={linkClass('/class-session')}>
                                Manage Classes
                            </Link>
                            <span className="hidden md:block font-sans text-sm text-parchment-300">
                                Welcome <strong className="text-parchment-100 font-medium">{authUser?.email}</strong>
                            </span>
                            <button
                                onClick={() => { logout(); }}
                                className="font-sans text-sm text-parchment-200 border border-parchment-300/30 rounded-md px-3 py-1.5 hover:bg-parchment-300/10 hover:text-parchment-100 transition-all duration-fast ease-zen"
                            >
                                {t("navbar.logout")}
                            </button>
                        </>
                    ) : (
                        <>
                            <Link to="/home" className={linkClass('/home')}>
                                {t("navbar.login")}
                            </Link>
                            <Link to="/register" className={linkClass('/register')}>
                                {t("navbar.register")}
                            </Link>
                        </>
                    )}

                </div>
            </div>
        </nav>
    );
}
