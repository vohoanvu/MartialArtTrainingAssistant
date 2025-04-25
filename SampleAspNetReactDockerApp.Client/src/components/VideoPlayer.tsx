import { useRef, useEffect, useCallback } from 'react';
import videojs from 'video.js';
import 'video.js/dist/video-js.css';
import 'videojs-markers-plugin';
import 'videojs-markers-plugin/dist/videojs.markers.plugin.css';

interface Feedback {
    id: number;
    timestamp: number;
    feedbackType: string;
    feedbackText: string;
}

interface VideoPlayerProps {
    videoUri: string;
    feedback: Feedback[];
    onTimeUpdate: (time: number) => void;
    onPause: () => void;
    seekTo: number | null;
}

const VideoPlayer: React.FC<VideoPlayerProps> = ({
    videoUri,
    feedback,
    onTimeUpdate,
    onPause,
    seekTo,
}) => {
    const videoRef = useRef<HTMLVideoElement | null>(null);
    const playerRef = useRef<any>(null);
    console.log('VideoPlayer component initialized with Feedback:', feedback, videoUri);

    // Memoized function to initialize Video.js
    const initializePlayer = useCallback(() => {
        if (!videoRef.current || !videoUri) return;

        playerRef.current = videojs(videoRef.current, {
            controls: true,
            playbackRates: [0.5, 1, 1.5, 2],
            fluid: true,
        });

        // Handle time updates
        playerRef.current.on('timeupdate', () => {
            onTimeUpdate(playerRef.current.currentTime());
        });

        // Handle pause event
        playerRef.current.on('pause', () => {
            onPause();
        });

        // Initialize markers plugin or additional configuration here if needed.
    }, [videoUri, onTimeUpdate, onPause]);

    // Callback ref to set videoRef and initialize player when element mounts
    const setVideoRef = useCallback((node: HTMLVideoElement | null) => {
        videoRef.current = node;
        if (node) {
            initializePlayer();
        }
    }, [initializePlayer]);

    // Cleanup on unmount
    useEffect(() => {
        return () => {
            if (playerRef.current) {
                playerRef.current.dispose();
                playerRef.current = null;
            }
        };
    }, []);

    // Handle seeking when seekTo changes
    useEffect(() => {
        if (seekTo !== null && playerRef.current) {
            playerRef.current.currentTime(seekTo);
        }
    }, [seekTo]);

    // Keyboard shortcuts
    useEffect(() => {
        const handleKey = (e: KeyboardEvent) => {
            if (!playerRef.current) return;

            switch (e.key) {
                case ' ':
                    e.preventDefault();
                    playerRef.current.paused()
                        ? playerRef.current.play()
                        : playerRef.current.pause();
                    break;
                case 'ArrowLeft':
                    playerRef.current.currentTime(playerRef.current.currentTime() - 5);
                    break;
                case 'ArrowRight':
                    playerRef.current.currentTime(playerRef.current.currentTime() + 5);
                    break;
                case 'f':
                    if (playerRef.current.paused()) {
                        onPause(); // Trigger feedback form
                    }
                    break;
                default:
                    break;
            }
        };

        window.addEventListener('keydown', handleKey);
        return () => window.removeEventListener('keydown', handleKey);
    }, [onPause]);

    return (
        <div className="w-full">
            {videoUri && (
                <div data-vjs-player>
                    <video ref={setVideoRef} className="video-js vjs-default-skin">
                        <source src={videoUri} type="video/mp4" />
                    </video>
                </div>
            )}
            <div className="h-5 bg-gray-300 relative mt-2" />
        </div>
    );
};

export default VideoPlayer;