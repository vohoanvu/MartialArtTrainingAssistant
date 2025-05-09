import { useState } from 'react';
import { uploadYoutubeVideo } from '@/services/api';
import useAuthStore from '@/store/authStore';
import { Button } from "@/components/ui/button.tsx";
import { Input } from '@/components/ui/input';
import VideoUploadForm from './VideoAnalysisEditor/VideoUploadForm';

const VideoSharingForm: React.FC = () => {
    const [videoUrl, setVideoUrl] = useState('');
    const { user, accessToken, hydrate } = useAuthStore();
    const [isLoading, setIsLoading] = useState(false);

    const uploadVideoRequest = async (videoUrl: string) => {
        try {
            const videoResponse = await uploadYoutubeVideo({
                videoUrl: videoUrl,
                jwtToken: accessToken,
                hydrate: () => { }
            });
            console.log('Video uploaded successfully:', videoResponse);
        } catch (error) {
            console.error('Error uploading video url:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const handleSubmit = async (event: { preventDefault: () => void; }) => {
        event.preventDefault();
        console.log('Video URL submitted:', videoUrl);

        await uploadVideoRequest(videoUrl);
        setVideoUrl('');
    };

    return (
        <div className="flex flex-col items-center space-y-8">
            <div className="w-full max-w-4xl my-10 p-6 border border-border rounded-lg shadow-lg bg-background">
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
            </div>

            {/* Video Upload Section - Only for authenticated users with role 0 or 1 */}
            {user && accessToken && (user.fighterInfo?.role === 0 || user.fighterInfo?.role === 1) && (
                <div className="w-full max-w-4xl">
                    <h2 className="text-2xl my-4 text-center">
                        Student Video Upload
                    </h2>
                    <div className="rounded-lg dark:shadow-accent p-4 transition duration-500 ease-in-out transform hover:scale-105">
                        <VideoUploadForm
                            fighterRole={user.fighterInfo?.role}
                            jwtToken={accessToken}
                            hydrateFn={hydrate}
                        />
                    </div>
                </div>
            )}
        </div>
    );
};

export default VideoSharingForm;