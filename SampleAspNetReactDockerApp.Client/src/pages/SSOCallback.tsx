import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import useAuthStore from "@/store/authStore";

const SsoCallback = () => {
    const navigate = useNavigate();
    const setTokens = useAuthStore((state) => state.setTokens);
    const setLoginStatus = useAuthStore((state) => state.setLoginStatus);
    const getUserInfo = useAuthStore((state) => state.getUserInfo);

    useEffect(() => {
        const params = new URLSearchParams(window.location.search);
        const accessToken = params.get("token");
        const refreshToken = params.get("refreshToken"); // Optionally support refreshToken if backend provides it

        if (accessToken) {
            setTokens(accessToken, refreshToken || "");
            setLoginStatus("authenticated");
            getUserInfo().finally(() => {
                navigate("/class-session");
            });
        } else {
            setLoginStatus("unauthenticated");
            navigate("/login?error=SSO-failed");
        }
    }, [navigate, setTokens, setLoginStatus, getUserInfo]);

    return <div>Signing you in...</div>;
};

export default SsoCallback;