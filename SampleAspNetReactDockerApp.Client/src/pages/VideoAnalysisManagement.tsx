import useAuthStore from '@/store/authStore';
import VideoUploadForm from '../components/VideoAnalysisEditor/VideoUploadForm';
import VideoStorageListing from './VideoStorageListing';

const VideoAnalysisManagement: React.FC = () => {
    //const [videoUrl, setVideoUrl] = useState('');
    const { user, accessToken, hydrate } = useAuthStore();
    //const [isLoading, setIsLoading] = useState(false);

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
                            />
                        </div>
                    </div>
                )}

                <div className="lg:w-2/3 w-full">
                    <VideoStorageListing />
                </div>
            </div>
        </div>
    );
};

export default VideoAnalysisManagement;

{/* Youtube Video AI Analysis is not included in MVP launch */}
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