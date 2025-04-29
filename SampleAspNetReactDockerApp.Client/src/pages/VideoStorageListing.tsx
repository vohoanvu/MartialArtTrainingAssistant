import { useState, useEffect } from 'react';
import { deleteUploadedVideo, getUploadedVideos } from '@/services/api';
import useAuthStore from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Trash2 } from 'lucide-react'; // Assuming you use lucide-react for icons
import { useNavigate } from 'react-router-dom';

export interface UploadedVideoDto {
    id: number;
    userId: string;
    filePath: string;
    description: string;
    uploadTimestamp: string;
    aiAnalysisResult: string;
    signedUrl: string;
}

const VideoStorageListing = () => {
    const [videos, setVideos] = useState<UploadedVideoDto[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const { accessToken, refreshToken, hydrate } = useAuthStore();
    const navigate = useNavigate();

    useEffect(() => {
        fetchVideos();
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const fetchVideos = async () => {
        setIsLoading(true);
        try {
            const videoList = await getUploadedVideos({
                jwtToken: accessToken,
                refreshToken,
                hydrate,
            });
            setVideos(videoList);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Unknown error');
        } finally {
            setIsLoading(false);
        }
    };

    const handleDelete = async (videoId: number) => {
        if (!confirm('Are you sure you want to delete this video?')) return;

        setIsLoading(true);
        try {
            await deleteUploadedVideo({
                videoId,
                jwtToken: accessToken,
                hydrate,
            });
            setVideos(videos.filter(v => v.id !== videoId));
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Unknown error');
        } finally {
            setIsLoading(false);
        }
    };

    const handleReview = (videoId: number) => {
        navigate(`/video-review/${videoId}`);
    };

    return (
        <div className="max-w-4xl mx-auto my-10 p-6 border rounded-lg shadow-lg bg-white">
            <h2 className="text-xl font-semibold text-gray-800 mb-4">Uploaded Videos</h2>
            {isLoading && <p>Loading...</p>}
            {error && <p className="text-red-500">{error}</p>}
            {!isLoading && !error && videos.length === 0 && <p>No videos uploaded yet.</p>}
            {!isLoading && videos.length > 0 && (
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead>Cloud Filepath</TableHead>
                            <TableHead>Description</TableHead>
                            <TableHead>Uploaded At</TableHead>
                            <TableHead>Action</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {videos.map(video => (
                            <TableRow key={video.id}>
                                <TableCell>{video.filePath}</TableCell>
                                <TableCell>{video.description}</TableCell>
                                <TableCell>{new Date(video.uploadTimestamp).toLocaleString()}</TableCell>
                                <TableCell className="flex space-x-2">
                                    <Button
                                        variant="destructive"
                                        size="icon"
                                        onClick={() => handleDelete(video.id)}
                                        disabled={isLoading}
                                    >
                                        <Trash2 className="h-4 w-4" />
                                    </Button>
                                    <Button
                                        variant="outline"
                                        size="sm"
                                        onClick={() => handleReview(video.id)}
                                    >
                                        Review
                                    </Button>
                                </TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            )}
        </div>
    );
};

export default VideoStorageListing;