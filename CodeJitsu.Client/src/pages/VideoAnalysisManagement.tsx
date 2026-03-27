import useAuthStore from '@/store/authStore';
import VideoUploadForm from '../components/VideoAnalysisEditor/VideoUploadForm';
import VideoStorageListing from './VideoStorageListing';
import { VideoUploadResponse } from '@/types/global';
import { useEffect, useState } from 'react';
import { useToast } from '@/hooks/use-toast';
import { analysisConnection } from '../services/SignalRService';
import * as signalR from '@microsoft/signalr';

const VideoAnalysisManagement: React.FC = () => {
    const { user, accessToken, hydrate } = useAuthStore();
    const [isUploading, setIsUploading] = useState(false);
    const [isAnalyzing, setIsAnalyzing] = useState(false);
    const [shouldRefreshList, setShouldRefreshList] = useState(false);
    const { toast } = useToast();
    const [isConnected, setIsConnected] = useState(false);

    useEffect(() => {
        if (isConnected || analysisConnection.state === signalR.HubConnectionState.Disconnected) {
            analysisConnection.start().then(() => {
                console.log('Connected to SignalR hub');
                setIsConnected(true);
                // Listen for the AnalysisCompleted event from the server.
                analysisConnection.on("AnalysisCompleted", (videoId: number) => {
                    toast({
                        title: "Analysis Completed",
                        description: `Video ${videoId} analysis is complete.`,
                        variant: "default",
                    });
                    setShouldRefreshList(true);
                });
            }).catch((err) => console.error("SignalR Connection Error on /analysisHub: ", err));
        }

        return () => {
            if (isConnected) {
                analysisConnection.stop();
                setIsConnected(false);
            }
        };
    }, [toast]);

    const handleUploadSuccess = async (response: VideoUploadResponse) => {
        try {
            setIsAnalyzing(true);
            const res = await fetch(`/vid/api/video/analyze/${response.videoId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${accessToken}`,
                },
            });

            if (res.status === 202) {
                // API accepted the request for background processing.
                toast({
                    title: "Analysis Started",
                    description: "Your video is being analyzed. This may take a few minutes...",
                    variant: "default",
                });
            } else if (!res.ok) {
                throw new Error('Analysis failed');
            } else {
                toast({
                    title: "Unexpected Response",
                    description: "Received an unexpected status from the analysis background job.",
                    variant: "destructive",
                });
            }
            setShouldRefreshList(true);
        } catch (error) {
            console.error('Analysis failed:', error);
            toast({
                title: "Analysis Failed",
                description: "There was an error analyzing your video. Please try again.",
                variant: "destructive",
            });
        } finally {
            setIsAnalyzing(false);
        }
    };

    return (
        <div className="container w-full max-auto p-4">
            <div className="flex flex-col lg:flex-row gap-8">
                {user && accessToken && (user.fighterInfo?.role === 0 || user.fighterInfo?.role === 1) && (
                    <div className="lg:w-1/3 w-full">
                        <div className="rounded-lg dark:shadow-accent p-4 bg-background">
                            <VideoUploadForm
                                fighterRole={user.fighterInfo?.role}
                                jwtToken={accessToken}
                                hydrateFn={hydrate}
                                isUploading={isUploading}
                                setIsUploading={setIsUploading}
                                onUploadSuccess={handleUploadSuccess}
                            />
                        </div>
                        {isAnalyzing && (
                            <div className="mt-4 flex items-center space-x-2">
                                <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-primary"></div>
                                <p className="text-primary">AI is analyzing the video. This may take a few minutes...</p>
                            </div>
                        )}
                    </div>
                )}

                <div className="lg:w-2/3 w-full">
                    <VideoStorageListing
                        shouldRefresh={shouldRefreshList}
                        onRefreshComplete={() => setShouldRefreshList(false)}
                    />
                </div>
            </div>
        </div>
    );
};

export default VideoAnalysisManagement;

{/* Youtube Video AI Analysis is not included in MVP launch */ }
// const uploadVideoRequest = async (videoUrl: string) => {
//     try {
//         const videoResponse = await uploadYoutubeVideo({
//             videoUrl: videoUrl,
//             jwtToken: accessToken,
//             hydrate: () => { }
//         });
//         console.log('Video uploaded successfully:', videoResponse);
//     } catch (error) {
//         console.error('Error uploading video url:', error);
//     } finally {
//         setIsLoading(false);
//     }
// };

// const handleSubmit = async (event: { preventDefault: () => void; }) => {
//     event.preventDefault();
//     console.log('Video URL submitted:', videoUrl);

//     await uploadVideoRequest(videoUrl);
//     setVideoUrl('');
// };
{/* <div className="w-full max-w-4xl my-10 p-6 border border-border rounded-lg shadow-lg bg-background">
    <form className="space-y-4" onSubmit={handleSubmit}>
        <h2 className="text-xl font-semibold text-foreground">Share a Youtube Video</h2>
        <div>
            <label htmlFor="videoUrl" className="block mb-2 text-sm font-medium text-foreground">Youtube URL</label>
            <Input
                type="text"
                id="videoUrl"
                name="videoUrl"
                value={videoUrl}
                onChange={(e) => setVideoUrl(e.target.value)}
                placeholder="Enter the Youtube URL here"
                className="w-full"
                required
                disabled={isLoading}
            />
        </div>
        <Button type="submit" size="lg" variant="outline" className="w-full">
            {isLoading ? 'Sharing...' : 'Share'}
        </Button>
    </form>
</div> */}