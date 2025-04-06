import { useState, useEffect } from 'react';
import { deleteUploadedVideo } from '@/services/api';
import useAuthStore from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Trash2 } from 'lucide-react'; // Assuming you use lucide-react for icons

interface UploadedVideo {
    Id: number;
    FilePath: string;
    Description: string;
    UploadTimestamp: string;
}

const VideoStorageListing = () => {
    const [videos, setVideos] = useState<UploadedVideo[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const { accessToken, hydrate } = useAuthStore();

    useEffect(() => {
        fetchVideos();
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const fetchVideos = async () => {
        setIsLoading(true);
        try {
            const response = await fetch('/vid/api/video/getall-uploaded', {
                headers: {
                    'Authorization': `Bearer ${accessToken}`,
                },
            });
            if (!response.ok) throw new Error('Failed to fetch videos');
            const data = await response.json();
            setVideos(data);
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
            setVideos(videos.filter(v => v.Id !== videoId));
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Unknown error');
        } finally {
            setIsLoading(false);
        }
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
                            <TableHead>ID</TableHead>
                            <TableHead>Description</TableHead>
                            <TableHead>Uploaded</TableHead>
                            <TableHead>Action</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {videos.map(video => (
                            <TableRow key={video.Id}>
                                <TableCell>{video.Id}</TableCell>
                                <TableCell>{video.Description}</TableCell>
                                <TableCell>{new Date(video.UploadTimestamp).toLocaleString()}</TableCell>
                                <TableCell>
                                    <Button
                                        variant="destructive"
                                        size="icon"
                                        onClick={() => handleDelete(video.Id)}
                                        disabled={isLoading}
                                    >
                                        <Trash2 className="h-4 w-4" />
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