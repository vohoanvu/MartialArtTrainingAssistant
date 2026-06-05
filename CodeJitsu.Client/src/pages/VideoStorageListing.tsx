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
    fighterName: string | null;
    martialArt: MartialArt;
    fighterId: number;
    studentIdentifier: string | null;
    transcodeStatus?: string; // None | Processing | Ready | Failed
}

interface VideoStorageListingProps {
    shouldRefresh: boolean;
    onRefreshComplete: () => void;
}

const VideoStorageListing = ({ shouldRefresh, onRefreshComplete }: VideoStorageListingProps) => {
    const [videos, setVideos] = useState<UploadedVideoDto[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [selectedAnalysis, setSelectedAnalysis] = useState<{ videoId: number; aiAnalysisResult: string } | null>(null);
    const { accessToken, refreshToken, hydrate } = useAuthStore();
    const navigate = useNavigate();

    useEffect(() => {
        // Fetch once per mount / shouldRefresh change; signal the parent when a requested refresh lands.
        fetchVideos().then(() => {
            if (shouldRefresh) onRefreshComplete();
        });
    }, [shouldRefresh]);

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
        setError(null);
        try {
            await deleteUploadedVideo({
                videoId,
                jwtToken: accessToken,
                hydrate,
            });
            // Optimistic removal for instant feedback, then re-sync with the server for the source of truth.
            setVideos(prev => prev.filter(v => v.id !== videoId));
            await fetchVideos();
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

    // const onImportAIAnalysis = async () => {
    //     if (!selectedAnalysis) return;

    //     const { videoId } = selectedAnalysis;

    //     setIsLoading(true);
    //     try {
    //         const response = await fetch(`/vid/api/video/import-ai/${videoId}`, {
    //             method: 'GET',
    //             headers: {
    //                 'Authorization': `Bearer ${accessToken}`,
    //                 'Content-Type': 'application/json',
    //             },
    //         });

    //         if (!response.ok) {
    //             const errorText = await response.text();
    //             throw new Error(`Failed to import AI analysis: ${errorText}`);
    //         }

    //         const data = await response.json();
    //         alert('AI analysis imported successfully!');
    //         console.log('Import AI Analysis response:', data);
    //     } catch (err) {
    //         setError(err instanceof Error ? err.message : 'Unknown error');
    //         alert('Failed to import AI analysi: ' + (err instanceof Error ? err.message : 'Unknown error'));
    //     } finally {
    //         setIsLoading(false);
    //         closeDialog();
    //     }
    // };

    return (
        <div className="max-w-6xl mx-auto my-5 p-4 border border-[rgba(60,50,40,0.10)] rounded-lg shadow-zen-sm bg-parchment-50">
            <h2 className="text-xl font-serif font-semibold mb-4 text-ink-400">Uploaded Videos</h2>
            {isLoading && <p className="text-slate-zen400">Loading...</p>}
            {error && <p className="text-blood-300">{error}</p>}
            {!isLoading && !error && videos.length === 0 && <p className="text-slate-zen400">No videos uploaded.</p>}
            {!isLoading && videos.length > 0 && (
                <Table className="w-full">
                    <TableHeader>
                        <TableRow>
                            <TableHead>Description</TableHead>
                            <TableHead>Uploaded At</TableHead>
                            <TableHead>Martial Art</TableHead>
                            <TableHead>Uploaded By</TableHead>
                            <TableHead>Action</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {videos.map(video => (
                            <TableRow key={video.id}>
                                <TableCell>{video.description}</TableCell>
                                <TableCell>{new Date(video.uploadTimestamp).toLocaleString()}</TableCell>
                                <TableCell>{video.martialArt}</TableCell>
                                <TableCell>{video.fighterName}</TableCell>
                                <TableCell className="flex space-x-2">
                                    <Button
                                        variant="danger"
                                        size="icon"
                                        onClick={() => handleDelete(video.id)}
                                        disabled={isLoading}
                                    >
                                        <Trash2 className="h-4 w-4" />
                                    </Button>
                                    <Button
                                        variant="primary"
                                        size="sm"
                                        onClick={() => handleReview(video.id)}
                                    >
                                        View Analysis Results
                                    </Button>
                                    <Button
                                        variant="secondary"
                                        size="sm"
                                        onClick={() => handleViewAnalysis(video.id, video.aiAnalysisResult)}
                                    >
                                        View JSON
                                    </Button>
                                </TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            )}

            {selectedAnalysis && (
                <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50">
                    <div className="bg-parchment-50 p-6 rounded-lg shadow-zen-lg max-w-6xl w-full max-h-[80vh] overflow-y-auto border border-[rgba(60,50,40,0.10)]">
                        <AiAnalysisResults analysisJson={selectedAnalysis.aiAnalysisResult} />
                        <div className="flex justify-between mt-4">
                            {/* <Button variant="default" onClick={onImportAIAnalysis}>Import AI Analysis</Button> */}
                            <Button variant="secondary" onClick={closeDialog}>Close</Button>
                        </div>
                        {error && <p className="text-blood-300 mt-2">{error}</p>}
                    </div>
                </div>
            )}
        </div>
    );
};

export default VideoStorageListing;