import { useRef, useEffect } from 'react';
import videojs from 'video.js';
import 'video.js/dist/video-js.css';

interface Feedback {
    id: number;
    timestamp: number;
    feedbackType: string;
    feedbackText: string;
}

interface VideoContainerProps {
    videoUri: string;
    feedback: Feedback[];
}

type VideoJsPlayer = ReturnType<typeof videojs>;

const VideoContainer: React.FC<VideoContainerProps> = ({ videoUri }) => {
    const videoRef = useRef<HTMLVideoElement | null>(null);
    const playerRef = useRef<VideoJsPlayer | null>(null);
    const containerRef = useRef<HTMLDivElement | null>(null);

    useEffect(() => {
        if (!videoRef.current || !containerRef.current) return;

        // Initialize Video.js only once
        if (!playerRef.current) {
            console.log('Initializing Video.js in VideoContainer');
            const player = videojs(
                videoRef.current,
                {
                    controls: true,
                    autoplay: false,
                    preload: 'auto',
                    fluid: true,
                    aspectRatio: '16:9',
                },
                () => {
                    console.log('Video.js player initialized');
                }
            );

            playerRef.current = player;

            player.on('error', () => {
                console.error('Video.js error:', player.error());
            });
        }

        // Update video source if it changes
        if (playerRef.current && videoUri) {
            console.log('Updating video source to:', videoUri);
            playerRef.current.src({ src: videoUri, type: 'video/mp4' });
        }

        // Cleanup on unmount
        return () => {
            if (playerRef.current) {
                console.log('Disposing Video.js player');
                playerRef.current.dispose();
                playerRef.current = null;
            }
        };
    }, [videoUri]); // Re-run when videoUri changes, but player persists

    return (
        <div
            ref={containerRef}
            style={{ width: '100%', height: '100%', minHeight: '400px' }}
        >
            <video
                ref={videoRef}
                className="video-js vjs-default-skin"
                style={{ width: '100%', height: '100%' }}
            />
        </div>
    );
};

export default VideoContainer;