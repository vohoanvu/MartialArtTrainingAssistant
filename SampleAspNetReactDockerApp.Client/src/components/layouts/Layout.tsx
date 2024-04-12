import {ReactElement} from "react";
import Navbar from "@/components/Navbar.tsx";
import {Outlet} from "react-router-dom";
import NotificationListener from "../NotificationsListener.tsx";

export function Layout(): ReactElement {
    return (
        <div className={`h-screen flex flex-col`}>
            <NotificationListener />
            <header className="border-b border-gray-400">
                <Navbar className="bg-gray-800 text-white shadow-md"/>
            </header>
            <main className={`flex-1`}>
                <Outlet />
            </main>
        </div>
    );
}