import { ReactElement } from "react";
import Navbar from "@/components/Navbar.tsx";
import { Outlet } from "react-router-dom";
import NotificationListener from "../NotificationsListener.tsx";

export function Layout(): ReactElement {
    return (
        <div className="min-h-screen flex flex-col bg-parchment-100">
            <NotificationListener />
            <header>
                <Navbar />
            </header>
            <main className="flex-1">
                <Outlet />
            </main>
        </div>
    );
}
