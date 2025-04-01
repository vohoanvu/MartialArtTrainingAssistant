import { useState } from 'react';
import axios from 'axios';
import { Button } from './ui/button';

interface VideoUploadFormProps {
    fighterRole: number; // 0 for Student, 1 for Instructor
    jwtToken: string;
}

const VideoUploadForm = ({ fighterRole, jwtToken }: VideoUploadFormProps) => {
    const [file, setFile] = useState<File | null>(null);
    const [description, setDescription] = useState('');
    const [signedUrl, setSignedUrl] = useState('');
    const [error, setError] = useState<string | null>(null);

    const uploadType = fighterRole === 0 ? 'sparring' : 'demonstration';
    const endpoint = fighterRole === 0 ? '/api/video/upload-sparring' : '/api/video/upload-demonstration';
    const title = fighterRole === 0 ? 'Upload Sparring Video' : 'Upload Demonstration Video';

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!file) {
            setError('Please select a video file');
            return;
        }

        setError(null);
        const formData = new FormData();
        formData.append('videoFile', file);
        formData.append('description', description);

        try {
            const response = await axios.post(endpoint, formData, {
                headers: {
                    'Authorization': `Bearer ${jwtToken}`,
                    'Content-Type': 'multipart/form-data',
                },
            });
            setSignedUrl(response.data.signedUrl);
            setFile(null);
            setDescription('');
        } catch (err) {
            const axiosError = err as { response?: { data?: { Message?: string } }, message?: string };
            setError('Upload failed: ' + (axiosError.response?.data?.Message || axiosError.message));
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
                    />
                </div>
                <Button
                    type="submit"
                    disabled={!file}
                    className="w-full bg-blue-500 text-white py-2 px-4 rounded-md hover:bg-blue-600 disabled:bg-gray-400"
                >
                    Upload {uploadType === 'sparring' ? 'Sparring' : 'Demonstration'} Video
                </Button>
            </form>
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