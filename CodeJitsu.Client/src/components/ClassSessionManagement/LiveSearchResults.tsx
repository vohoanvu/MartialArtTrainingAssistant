import React, { useState, useEffect } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { YoutubeIcon } from 'lucide-react';

interface YoutubeVideo {
    title: string;
    description: string;
    video_id: string;
    publication_date: string;
    embed_link: string;
    thumbnail_url: string;
}

interface SearchResults {
    youtube_videos: YoutubeVideo[];
}

interface LiveSearchResultsProps {
    jsonContent: string;
}

const LiveSearchResults: React.FC<LiveSearchResultsProps> = ({ jsonContent }) => {
    const [data, setData] = useState<SearchResults | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [currentVideo, setCurrentVideo] = useState<string | null>(null);

    useEffect(() => {
        try {
            const parsedData = JSON.parse(jsonContent);
            setData(parsedData);
        } catch (err) {
            setError('Failed to parse search results');
        }
    }, [jsonContent]);

    if (error) {
        return <div className="text-red-500">{error}</div>;
    }

    if (!data) {
        return <div>Loading...</div>;
    }

    const openVideoModal = (embedLink: string) => {
        setCurrentVideo(embedLink);
        setIsModalOpen(true);
    };

    return (
        <div className="space-y-8">
            {data.youtube_videos?.length > 0 && (
                <section>
                    <h2 className="text-2xl font-bold mb-4 flex items-center">
                        <YoutubeIcon className="mr-2" /> YouTube Videos
                    </h2>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                        {data.youtube_videos.map((video, index) => (
                            <VideoCard key={index} video={video} onPlay={() => openVideoModal(video.embed_link)} />
                        ))}
                    </div>
                </section>
            )}

            <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
                <DialogContent className="max-w-4xl">
                    <DialogHeader>
                        <DialogTitle>Video Player</DialogTitle>
                    </DialogHeader>
                    {currentVideo && (
                        <iframe
                            width="100%"
                            height="400"
                            src={currentVideo}
                            frameBorder="0"
                            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                            allowFullScreen
                        ></iframe>
                    )}
                </DialogContent>
            </Dialog>
        </div>
    );
};

const VideoCard: React.FC<{ video: YoutubeVideo; onPlay: () => void }> = ({ video, onPlay }) => {
    const thumbnailUrl = video.thumbnail_url || `https://img.youtube.com/vi/${video.video_id}/0.jpg`;
    return (
        <div className="bg-card p-4 rounded-lg shadow hover:shadow-md transition-shadow">
            <img src={thumbnailUrl} alt={video.title} className="w-full h-48 object-cover rounded" />
            <h3 className="text-lg font-semibold mt-2 text-card-foreground">{video.title}</h3>
            <p className="text-sm text-muted-foreground">{video.publication_date}</p>
            <p className="text-sm mt-2 text-card-foreground">{video.description}</p>
            <Button onClick={onPlay} className="mt-2">Play Video</Button>
        </div>
    );
};

export default LiveSearchResults;