import { useState } from 'react';
import { uploadVideoFile } from '@/services/api'; // Import the new function
import { Button } from './ui/button';
import { VideoUploadResponse } from '@/types/global';

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

        try {
            const response: VideoUploadResponse = await uploadVideoFile({
                file,
                description,
                uploadType,
                jwtToken,
                hydrate: hydrateFn,
                onProgress: (percent: number) => setProgress(percent)
            });
            setSignedUrl(response.SignedUrl);
            setFile(null);
            setDescription('');
            console.log(`${uploadType} video uploaded successfully:`, response);
        } catch (err) {
            const errorMessage = err instanceof Error ? err.message : 'Unknown error';
            setError(`Upload failed: ${errorMessage}`);
            console.error(`Error uploading ${uploadType} video:`, err);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="w-full max-w-md p-4 bg-gray-100 rounded-lg shadow-md">
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
        </div>
    );
};

export default VideoUploadForm;