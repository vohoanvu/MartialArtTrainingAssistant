// SampleAspNetReactDockerApp.Client/src/pages/SSOCallback.tsx
import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import useAuthStore from "@/store/authStore";

const SsoCallback = () => {
    const navigate = useNavigate();
    const { setTokens, setLoginStatus, getUserInfo } = useAuthStore();

    useEffect(() => {
        const params = new URLSearchParams(window.location.search);
        const accessToken = params.get("token");
        const refreshToken = params.get("refreshToken");
        const error = params.get("error");

        if (error) {
            setLoginStatus("unauthenticated");
            navigate(`/login?error=${encodeURIComponent(error)}`);
        } else if (accessToken && refreshToken) {
            setTokens(accessToken, refreshToken);
            setLoginStatus("authenticated");
            getUserInfo().finally(() => {
                navigate("/class-session");
            });
        } else {
            setLoginStatus("unauthenticated");
            navigate("/login?error=SSO-failed-unexpected");
        }
    }, [navigate, setTokens, setLoginStatus, getUserInfo]);

    return <div>Signing you in...</div>;
};

export default SsoCallback;