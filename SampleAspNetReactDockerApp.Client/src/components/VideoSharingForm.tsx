import { useState } from 'react';
import { uploadYoutubeVideo } from '@/services/api';
import useAuthStore from '@/store/authStore';
import {Button} from "@/components/ui/button.tsx";

const VideoSharingForm : React.FC = () =>
{
    const [videoUrl, setVideoUrl] = useState('');
    const { accessToken } = useAuthStore();
    const [isLoading, setIsLoading] = useState(false);

    const uploadVideoRequest = async (videoUrl : string) => {
        try {
            const videoResponse = await uploadYoutubeVideo({
                videoUrl: videoUrl,
                jwtToken: accessToken,
                hydrate: () => {}
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
        <div className="max-w-md mx-auto my-10 p-6 border rounded-lg shadow-lg bg-white">
            <form className="space-y-4" onSubmit={handleSubmit}>
                <h2 className="text-xl font-semibold text-gray-800">Share a Youtube Video</h2>
                <div>
                    <label htmlFor="videoUrl" className="block mb-2 text-sm font-medium text-gray-700">Youtube URL</label>
                    <input
                        type="text"
                        id="videoUrl"
                        name="videoUrl"
                        value={videoUrl}
                        onChange={(e) => setVideoUrl(e.target.value)}
                        placeholder="Enter the Youtube URL here"
                        className="w-full p-2 border border-gray-300 rounded-lg focus:ring-blue-500 focus:border-blue-500"
                        required
                    />
                </div>
                <Button type="submit" size="lg" variant="outline" className="w-full p-3 focus:ring-4">
                    {isLoading ? 'Sharing...' : 'Share'}
                </Button>
            </form>
        </div>
    );
};

export default VideoSharingForm;