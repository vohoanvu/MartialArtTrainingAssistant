import {ReactElement} from "react";
import {ModeToggle} from "@/components/mode-toggle.tsx";
import {cn} from "@/lib/utils.ts";
import {useTranslation} from "react-i18next";
import {Link} from "react-router-dom";
import useAuthStore from "@/store/authStore.ts";
import {Button} from "@/components/ui/button.tsx";

/**
 * Navbar component
 */
export default function Navbar(
    {
        className = "",
    }
): ReactElement {

    const {t} = useTranslation();
    const authStatus = useAuthStore((state) => state.loginStatus);
    const logout = useAuthStore((state) => state.logout);
    const authUser = useAuthStore((state) => state.user);
    
    return (
        <div className={cn(className)}>
            <nav className="bg-background text-foreground p-4">
                <div className="container mx-auto flex justify-between items-center">
                    <div className="text-lg font-bold flex items-center space-x-2">
                        <img
                            src="/codejitsu-logo-2-round.png"
                            alt="CodeJitsu Logo"
                            className="h-12 w-12"
                        />
                        <a href="/" className="hover:text-primary">
                            CodeJitsu
                        </a>
                    </div>
                    <ul className="flex space-x-5 mt-4">
                        <li>
                            <Link to="/pricing" className="hover:text-primary">
                                Pricing
                            </Link>
                        </li>
                        {authStatus === "authenticated" ?
                            (
                                <>
                                    <li className="">
                                        <Link to="/video-analysis">
                                            <Button size="sm" variant="outline" className="w-full">
                                                Video Analysis
                                            </Button>
                                        </Link>
                                    </li>
                                    <li>
                                        <Link to="/class-session">
                                            <Button size="sm" variant="outline" className="w-full">
                                                Manage Classes
                                            </Button>
                                        </Link>
                                    </li>
                                    <li>
                                        Welcome <strong>{authUser?.email}!</strong>
                                    </li>
                                    <li>
                                        <button onClick={() => { logout(); }}>
                                            {t("navbar.logout")}
                                        </button>
                                    </li>
                                </>
                            )
                            : (
                                <>
                                    <li>
                                        <Link to="/home" className="hover:text-primary">
                                            {t("navbar.login")}
                                        </Link>
                                    </li>
                                    <li>
                                        <Link to="/register" className="hover:text-primary">
                                            {t("navbar.register")}
                                        </Link>
                                    </li>
                                    {/* <li>
                                        <Link to="/contact" className="hover:text-primary">
                                            {t("navbar.contact")}
                                        </Link>
                                    </li> */}
                                </>
                            )
                        }

                        <li>
                            <ModeToggle className={cn("h-8 w-8")}/>
                        </li>
                        {/* <li>
                            <LangToggle className={cn("h-8")}/>
                        </li> */}
                    </ul>
                </div>
            </nav>
        </div>
    );
}
