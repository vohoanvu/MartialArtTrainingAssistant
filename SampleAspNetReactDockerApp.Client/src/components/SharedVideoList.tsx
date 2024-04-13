import { useState, useEffect, useCallback } from 'react';
import { SharedVideo } from '@/types/global';
import useAuthStore from "@/store/authStore.ts";
import { getAllSharedVideos } from '@/services/api';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from './ui/card';

const SharedVideosList: React.FC = () => 
{
    const [videos, setVideos] = useState<SharedVideo[]>([]);
    const { accessToken, refreshToken } = useAuthStore();

    // Fetch the list of shared videos from the API
    const fetchVideos = useCallback(async () => {
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
    }, [accessToken, refreshToken]);

    useEffect(() => {
        fetchVideos();
    }, [fetchVideos]);

    return (
        <div className="flex flex-col gap-4 p-4">
            {videos.length > 0 ? (
                videos.map((video) => (
                    <Card key={video.id} className="flex gap-4 bg-white p-4 rounded shadow">
                        <div className="flex-none w-1/2">
                            <iframe
                                className="w-full h-full"
                                src={video.embedLink}
                                title={video.title}
                                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                                allowFullScreen
                            ></iframe>
                        </div>
                        <div className="flex-grow">
                            <Card>
                                <CardHeader>
                                    <CardTitle>{video.title}</CardTitle>
                                    <CardContent>Shared by: {video.sharedBy?.username}</CardContent>
                                    <CardDescription>Description:<br/> {video.description}</CardDescription>
                                </CardHeader>
                            </Card>
                        </div>
                    </Card>
                ))
            ) : (
                <p className="text-center text-gray-800">Loading data from API...</p>
            )}
        </div>
    );
};

export default SharedVideosList;