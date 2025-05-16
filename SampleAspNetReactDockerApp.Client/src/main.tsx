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
import SharingVideo from '@/pages/SharingVideo.tsx';
import TrainingSessionForm from './components/ClassSessionManagement/TrainingSessionForm.tsx';
import TrainingSessionDetails from './components/ClassSessionManagement/TrainingSessionDetails.tsx';
import VideoShareList from './pages/VideoShareList.tsx';
import VideoStorageListing from './pages/VideoStorageListing.tsx';
import VideoReview from './pages/VideoReview.tsx';
import LandingPage from './pages/LandingPage.tsx';
import SsoCallback from './pages/SSOCallback.tsx';

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
                element: <Login/>,
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
                path: '/share-video',
                element: <SharingVideo/>,
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
                path: '/video-listing',
                element: <VideoStorageListing/>,
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
    </React.StrictMode>,
)
