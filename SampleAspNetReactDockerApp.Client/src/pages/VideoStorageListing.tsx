import { useState, useEffect } from 'react';
import { deleteUploadedVideo, getUploadedVideos } from '@/services/api';
import useAuthStore from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Trash2 } from 'lucide-react'; // Assuming you use lucide-react for icons
import { useNavigate } from 'react-router-dom';
import { MartialArt } from '@/types/global';
import AiAnalysisResults from '@/components/AiAnalysisResults';

export interface UploadedVideoDto {
    id: number;
    userId: string;
    filePath: string;
    description: string;
    uploadTimestamp: string;
    aiAnalysisResult: string;
    signedUrl: string;
    studentIdentifier: string;
    martialArt: MartialArt;
}

const VideoStorageListing = () => {
    const [videos, setVideos] = useState<UploadedVideoDto[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [selectedAnalysis, setSelectedAnalysis] = useState<string | null>(null);
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

    const handleViewAnalysis = (aiAnalysisResult: string) => {
        setSelectedAnalysis(aiAnalysisResult);
    };

    const closeDialog = () => {
        setSelectedAnalysis(null);
    };

    return (
        <div className="max-w-6xl mx-auto my-10 p-6 border rounded-lg shadow-lg bg-white">
            <h2 className="text-xl font-semibold text-gray-800 mb-4">Uploaded Videos</h2>
            {isLoading && <p>Loading...</p>}
            {error && <p className="text-red-500">{error}</p>}
            {!isLoading && !error && videos.length === 0 && <p>No videos uploaded yet.</p>}
            {!isLoading && videos.length > 0 && (
                <Table className="w-full">
                    <TableHeader>
                        <TableRow>
                            <TableHead>Cloud Filepath</TableHead>
                            <TableHead>Description</TableHead>
                            <TableHead>Uploaded At</TableHead>
                            <TableHead>Martial Art</TableHead>
                            <TableHead>Student Identifier</TableHead>
                            <TableHead>Action</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {videos.map(video => (
                            <TableRow key={video.id}>
                                <TableCell>{video.filePath}</TableCell>
                                <TableCell>{video.description}</TableCell>
                                <TableCell>{new Date(video.uploadTimestamp).toLocaleString()}</TableCell>
                                <TableCell>{video.martialArt}</TableCell>
                                <TableCell>{video.studentIdentifier}</TableCell>
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
                                        variant="default"
                                        size="sm"
                                        onClick={() => handleReview(video.id)}
                                    >
                                        Review
                                    </Button>
                                    <Button
                                        variant="outline"
                                        size="sm"
                                        onClick={() => handleViewAnalysis(video.aiAnalysisResult)}
                                    >
                                        View AI Analysis
                                    </Button>
                                </TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            )}

            {selectedAnalysis && (
                <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center">
                    <div className="bg-white p-6 rounded-lg shadow-lg max-w-6xl w-full max-h-[80vh] overflow-y-auto">
                        <AiAnalysisResults analysisJson={selectedAnalysis} />
                        <div className="flex justify-end mt-4">
                            <Button variant="ghost" onClick={closeDialog}>Close</Button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default VideoStorageListing;