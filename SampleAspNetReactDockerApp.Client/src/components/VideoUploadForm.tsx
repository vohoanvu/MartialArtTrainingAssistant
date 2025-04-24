import { useState } from 'react';
import { uploadVideoFile } from '@/services/api'; // Import the new function
import { Button } from './ui/button';
import { VideoUploadResponse } from '@/types/global';
import AiAnalysisResults from './AiAnalysisResults';

interface VideoUploadFormProps {
    fighterRole: number; // 0 for Student, 1 for Instructor
    jwtToken: string;
    hydrateFn: () => Promise<void>;
}

const VideoUploadForm = ({ fighterRole, jwtToken, hydrateFn }: VideoUploadFormProps) => {
    const [file, setFile] = useState<File | null>(null);
    const [description, setDescription] = useState('');
    const [signedUrl, setSignedUrl] = useState('');
    const [isLoading, setIsLoading] = useState(false); // Added for loading state
    const [error, setError] = useState<string | null>(null);
    const [progress, setProgress] = useState(0);

    const [analysisLoading, setAnalysisLoading] = useState(false);
    const [analysisCompleted, setAnalysisCompleted] = useState(false);
    //const [analysisVideoId, setAnalysisVideoId] = useState<number | null>(null);
    const [analysisResult, setAnalysisResult] = useState<any>(null);

    const uploadType = fighterRole === 0 ? 'sparring' : 'demonstration';
    const title = fighterRole === 0 ? 'Upload Sparring Video' : 'Upload Demonstration Video';

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!file) {
            setError('Please select a video file');
            return;
        }

        setIsLoading(true);
        setError(null);
        setProgress(0);
        setAnalysisCompleted(false);
        //setAnalysisVideoId(null);
        setAnalysisResult(null);

        try {
            const response: VideoUploadResponse = await uploadVideoFile({
                file,
                description,
                uploadType,
                jwtToken,
                hydrate: hydrateFn,
                onProgress: (percent: number) => setProgress(percent)
            });
            setSignedUrl(response.signedUrl);
            console.log('SignedUrl response expected as GCS path:', response.signedUrl);
            // Save the VideoId to trigger analysis later
            const videoId = response.videoId;
            //setAnalysisVideoId(videoId);
            console.log('VideoId response expected:', videoId);
            setFile(null);
            setDescription('');
            console.log(`${uploadType} video upload response:`, response);

            // After successful upload, trigger analysis AI asynchronously
            setAnalysisLoading(true);
            (async () => {
                try {
                    const res = await fetch('/vid/api/video/analyze', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Authorization': `Bearer ${jwtToken}`
                        },
                        body: JSON.stringify({
                            VideoId: videoId,
                        })
                    });
                    if (!res.ok) {
                        console.error('Analysis API call failed');
                    } else {
                        const resultData = await res.json();
                        console.log('Analysis completed successfully.', resultData);
                        setAnalysisResult(resultData);
                        setAnalysisCompleted(true);
                    }
                } catch (err) {
                    const errorMessage = err instanceof Error ? err.message : 'Unknown error';
                    setError(`Upload failed: ${errorMessage}`);
                    console.error(`Error uploading ${uploadType} video:`, err);
                } finally {
                    setIsLoading(false);
                }
            })();
        } catch (err) {
            const errorMessage = err instanceof Error ? err.message : 'Unknown error';
            setError(`Upload failed: ${errorMessage}`);
            console.error(`Error uploading ${uploadType} video:`, err);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="w-full max-w p-4 bg-gray-100 rounded-lg shadow-md">
            <h2 className="text-xl font-bold mb-4">{title}</h2>
            <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                    <label htmlFor="videoFile" className="block text-sm font-medium text-gray-700">
                        Video File (MP4 or AVI)
                    </label>
                    <input
                        type="file"
                        id="videoFile"
                        accept="video/mp4,video/avi"
                        onChange={(e) => setFile(e.target.files?.[0] || null)}
                        className="mt-1 block w-full border rounded-md p-2"
                        disabled={isLoading}
                    />
                </div>
                <div>
                    <label htmlFor="description" className="block text-sm font-medium text-gray-700">
                        Description
                    </label>
                    <textarea
                        id="description"
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        placeholder="Enter a description"
                        className="mt-1 block w-full border rounded-md p-2"
                        rows={4}
                        disabled={isLoading}
                    />
                </div>
                <Button
                    type="submit"
                    disabled={!file || isLoading}
                    className="w-full bg-blue-500 text-white py-2 px-4 rounded-md hover:bg-blue-600 disabled:bg-gray-400"
                >
                    {isLoading ? 'Uploading...' : `Upload ${uploadType === 'sparring' ? 'Sparring' : 'Demonstration'} Video`}
                </Button>
            </form>
            {isLoading && (
                <div className="mt-4">
                    <progress value={progress} max="100" className="w-full" />
                    <p className="text-center mt-1">{progress}%</p>
                </div>
            )}
            {error && <p className="text-red-500 mt-2">{error}</p>}
            {signedUrl && (
                <div className="mt-4">
                    <p className="text-green-500">Upload successful!</p>
                    <video src={signedUrl} controls className="w-full mt-2" />
                </div>
            )}

            {/* Analysis progress notification */}
            {analysisLoading && !analysisCompleted && (
                <div className="mt-4 flex items-center space-x-2">
                    <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-blue-500"></div>
                    <p className="text-blue-500">Your video is being analyzed. This may take a few minutes.</p>
                </div>
            )}
            {analysisCompleted && analysisResult && (
                <AiAnalysisResults analysisJson={analysisResult.analysisJson} />
            )}
            {/* {analysisCompleted && analysisVideoId && (
                <div className="mt-4">
                    <Button
                        onClick={() => console.log(`Navigating to the analysis results page with video ID: ${analysisVideoId} .....`)}
                        className="w-full bg-green-500 text-white py-2 px-4 rounded-md hover:bg-green-600"
                    >
                        View AI Analysis Results
                    </Button>
                </div>
            )} */}
        </div>
    );
};

export default VideoUploadForm;