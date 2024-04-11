import React, { useState, useEffect } from 'react';
import { SharedVideo } from '@/types/global';
import useAuthStore from "@/store/authStore.ts";
import { getAllSharedVideos } from '@/services/api';

const SharedVideosList: React.FC = () => {
    const [videos, setVideos] = useState<SharedVideo[]>([]);
    const { accessToken, refreshToken } = useAuthStore();

    useEffect(() => {
        // Fetch the list of shared videos from the API
        const fetchVideos = async () => {
            try {
                const videoList = await getAllSharedVideos({
                    jwtToken: accessToken,
                    refreshToken,
                    hydrate: () => {} // Implement the hydrate function to refresh tokens
                });
                setVideos(videoList);
            } catch (error) {
                console.error('Error fetching videos:', error);
            }
        };

        fetchVideos();
    }, [accessToken, refreshToken]);

    return (
        <div className="flex flex-col gap-4 p-4">
            {videos.length > 0 ? (
                videos.map((video) => (
                    <div key={video.id} className="flex gap-4 bg-white p-4 rounded shadow">
                    <div className="flex-none w-1/3">
                        <iframe
                            className="w-full h-full"
                            src={video.embedLink}
                            title={video.title}
                            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                            allowFullScreen
                        ></iframe>
                    </div>
                    <div className="flex-grow">
                        <h3 className="text-lg text-gray-900 font-bold">{video.title}</h3>
                        <p className="text-sm text-gray-600">Shared by: {video.sharedBy?.username}</p>
                        <p className="text-sm text-gray-800">Description:<br/>  {video.description}</p>
                        {/* Add other video details as needed */}
                    </div>
                    </div>
                ))
            ) : (
                <p className="text-center text-gray-800">Loading data from API...</p>
            )}
        </div>
    );
};

export default SharedVideosList;