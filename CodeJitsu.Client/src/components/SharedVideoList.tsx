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
                    <Card key={video.id} className="flex gap-4 bg-background p-4 rounded shadow border border-border">
                        <div className="flex-none w-1/2">
                            <iframe
                                className="w-full aspect-video rounded border border-border"
                                src={video.embedLink}
                                title={video.title}
                                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                                allowFullScreen
                            ></iframe>
                        </div>
                        <div className="flex-grow">
                            <Card className="bg-background border-none shadow-none p-0">
                                <CardHeader>
                                    <CardTitle className="text-foreground">{video.title}</CardTitle>
                                    <CardContent className="text-muted-foreground">Shared by: {video.sharedBy?.username}</CardContent>
                                    <CardDescription className="text-foreground">Description:<br/> {video.description}</CardDescription>
                                </CardHeader>
                            </Card>
                        </div>
                    </Card>
                ))
            ) : (
                <p className="text-center text-muted-foreground">No Video yet shared!</p>
            )}
        </div>
    );
};

export default SharedVideosList;