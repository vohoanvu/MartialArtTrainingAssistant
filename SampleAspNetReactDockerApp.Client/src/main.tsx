import React from 'react'
import ReactDOM from 'react-dom/client'
import './index.css'
import {createBrowserRouter, RouterProvider} from "react-router-dom";
import {Home} from "@/pages/Home.tsx";
import {Layout} from "@/components/layouts/Layout.tsx";
import "./services/i18n.ts";
import {initLang} from "@/store/langStore.ts";
import Login from "@/pages/Login.tsx";
import ClassSession from "@/pages/Dashboard.tsx";
import About from "@/pages/About.tsx";
import Register from "@/pages/Register.tsx";
import Contact from "@/pages/Contact.tsx";
import TrainingSessionForm from './components/ClassSessionManagement/TrainingSessionForm.tsx';
import TrainingSessionDetails from './components/ClassSessionManagement/TrainingSessionDetails.tsx';
import VideoShareList from './pages/VideoShareList.tsx';
import VideoReview from './pages/VideoReview.tsx';
import LandingPage from './pages/LandingPage.tsx';
import SsoCallback from './pages/SSOCallback.tsx';
import { Toaster } from './components/ui/toaster.tsx';
import VideoAnalysisManagement from './pages/VideoAnalysisManagement.tsx';

const router = createBrowserRouter([
    {
        path: '/',
        element: <Layout/>,
        children: [
            {
                path: '/',
                element: <LandingPage/>,
            },
            {
                path: '/home',
                element: <Home/>,
            },
            {
                path: '/about',
                element: <About/>
            },
            {
                path: '/login',
                element: <Home/>,
            },
            {
                path: '/register',
                element: <Register/>,
            },
            {
                path: '/class-session',
                element: <ClassSession/>,
            },
            {
                path: '/contact',
                element: <Contact/>,
            },
            {
                path: '/video-analysis',
                element: <VideoAnalysisManagement/>,
            },
            {
                path: '/create-session',
                element: <TrainingSessionForm/>,
            },
            {
                path: '/edit-session/:sessionId',
                element: <TrainingSessionForm/>
            },
            {
                path: '/session-details/:sessionId',
                element: <TrainingSessionDetails/>
            },
            {
                path: '/videos',
                element: <VideoShareList/>,
            },
            {
                path: '/video-review/:videoId',
                element: <VideoReview/>,
            },
            {
                path: '/sso-callback',
                element: <SsoCallback/>
            }
        ]
    }
]);

initLang();

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <RouterProvider router={router}/>
        <Toaster />
    </React.StrictMode>,
)
