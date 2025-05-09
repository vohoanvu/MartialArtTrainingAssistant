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
    fighterId: number;
}

const VideoStorageListing = () => {
    const [videos, setVideos] = useState<UploadedVideoDto[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [selectedAnalysis, setSelectedAnalysis] = useState<{ videoId: number; aiAnalysisResult: string } | null>(null);
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

    const handleViewAnalysis = (videoId: number, aiAnalysisResult: string) => {
        setSelectedAnalysis({ videoId, aiAnalysisResult });
    };

    const closeDialog = () => {
        setSelectedAnalysis(null);
    };

    const onImportAIAnalysis = async () => {
        if (!selectedAnalysis) return;

        const { videoId } = selectedAnalysis;

        setIsLoading(true);
        try {
            const response = await fetch(`/vid/api/video/import-ai/${videoId}`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${accessToken}`,
                    'Content-Type': 'application/json',
                },
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`Failed to import AI analysis: ${errorText}`);
            }

            const data = await response.json();
            alert('AI analysis imported successfully!');
            console.log('Import AI Analysis response:', data);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Unknown error');
            alert('Failed to import AI analysi: ' + (err instanceof Error ? err.message : 'Unknown error'));
        } finally {
            setIsLoading(false);
            closeDialog();
        }
    };

    return (
        <div className="max-w-6xl mx-auto my-10 p-6 border rounded-lg shadow-lg">
            <h2 className="text-xl font-semibold mb-4">Uploaded Videos</h2>
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
                                        onClick={() => handleViewAnalysis(video.id, video.aiAnalysisResult)}
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
                        <AiAnalysisResults analysisJson={selectedAnalysis.aiAnalysisResult} />
                        <div className="flex justify-between mt-4">
                            <Button variant="default" onClick={onImportAIAnalysis}>Import AI Analysis</Button>
                            <Button variant="secondary" onClick={closeDialog}>Close</Button>
                        </div>
                        {error && <p className="text-red-500">{error}</p>}
                    </div>
                </div>
            )}
        </div>
    );
};

export default VideoStorageListing;