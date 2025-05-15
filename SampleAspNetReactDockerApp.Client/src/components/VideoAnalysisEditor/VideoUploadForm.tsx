import { useState } from 'react';
import { uploadVideoFile } from '@/services/api';
import { Button } from '../ui/button';
import { VideoUploadResponse, MartialArt } from '@/types/global';
import AiAnalysisResults from '../AiAnalysisResults';
import { Input } from '../ui/input';
import { Textarea } from '../ui/textarea';
import { Select, SelectTrigger, SelectContent, SelectItem, SelectValue } from '../ui/select';

interface VideoUploadFormProps {
    fighterRole: number; // 0 for Student, 1 for Instructor
    jwtToken: string;
    hydrateFn: () => Promise<void>;
}

const VideoUploadForm = ({ fighterRole, jwtToken, hydrateFn }: VideoUploadFormProps) => {
    const [file, setFile] = useState<File | null>(null);
    const [description, setDescription] = useState('');
    const [studentIdentifier, setStudentIdentifier] = useState('');
    const [martialArt, setMartialArt] = useState<MartialArt>(MartialArt.BrazilianJiuJitsu_GI);
    const [signedUrl, setSignedUrl] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [progress, setProgress] = useState(0);

    const [analysisLoading, setAnalysisLoading] = useState(false);
    const [analysisCompleted, setAnalysisCompleted] = useState(false);
    const [analysisResult, setAnalysisResult] = useState<any>(null);

    const uploadType = fighterRole === 0 ? 'sparring' : 'demonstration';
    const title = fighterRole === 0 ? 'Upload Sparring Video' : 'Upload Demonstration Video';

    const martialArtOptions = Object.values(MartialArt);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!file) {
            setError('Please select a video file');
            return;
        }
        if (!studentIdentifier) {
            setError('Please provide a student identifier');
            return;
        }

        setIsLoading(true);
        setError(null);
        setProgress(0);
        setAnalysisCompleted(false);
        setAnalysisResult(null);

        try {
            const response: VideoUploadResponse = await uploadVideoFile({
                file,
                description,
                studentIdentifier,
                martialArt,
                uploadType,
                jwtToken,
                hydrate: hydrateFn,
                onProgress: (percent: number) => setProgress(percent)
            });
            setSignedUrl(response.signedUrl);
            console.log('SignedUrl response expected as GCS path:', response.signedUrl);
            const videoId = response.videoId;
            console.log('VideoId response expected:', videoId);
            setFile(null);
            setDescription('');
            setStudentIdentifier('');
            setMartialArt(MartialArt.None);
            console.log(`${uploadType} video upload response:`, response);

            // Trigger analysis AI asynchronously
            setAnalysisLoading(true);
            (async () => {
                try {
                    const res = await fetch(`/vid/api/video/analyze/${videoId}`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            Authorization: `Bearer ${jwtToken}`,
                        },
                    });
                    if (!res.ok) {
                        console.error('Analysis API call failed');
                    } else {
                        const resultData = await res.json();
                        console.log('Analysis completed successfully.', resultData);
                        setAnalysisResult(resultData);
                        setAnalysisCompleted(true);
                    }
                } catch (err) {
                    const errorMessage = err instanceof Error ? err.message : 'Unknown error';
                    setError(`Upload failed: ${errorMessage}`);
                    console.error(`Error uploading ${uploadType} video:`, err);
                } finally {
                    setIsLoading(false);
                }
            })();
        } catch (err : any) {
            if (err.signedUrl) {
                alert(`Duplicate video detected: ${err.message}. You can access the existing video here: ${err.signedUrl}`);
            } else {
                const errorMessage = err instanceof Error ? err.message : 'Unknown error';
                setError(`Upload failed: ${errorMessage}`);
                console.error(`Error uploading ${uploadType} video:`, err.message);
            }
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="w-full max-w p-4 bg-background rounded-lg shadow-md border border-border">
            <h2 className="text-xl font-bold mb-4 text-foreground">{title}</h2>
            <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                    <label htmlFor="videoFile" className="block text-sm font-medium text-foreground">
                        Video File (MP4 or AVI)
                    </label>
                    <Input
                        type="file"
                        id="videoFile"
                        accept="video/mp4,video/avi,video/mov,video/mpeg,video/webm"
                        onChange={(e) => setFile(e.target.files?.[0] || null)}
                        className="mt-1"
                        disabled={isLoading}
                    />
                </div>
                <div>
                    <label htmlFor="description" className="block text-sm font-medium text-foreground">
                        Description
                    </label>
                    <Textarea
                        id="description"
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        placeholder="Optional: Enter a description"
                        className="mt-1"
                        rows={4}
                        disabled={isLoading}
                    />
                </div>
                <div>
                    <label htmlFor="studentIdentifier" className="block text-sm font-medium text-foreground">
                        Specify which fighter are you in the video?
                    </label>
                    <Input
                        type="text"
                        id="studentIdentifier"
                        value={studentIdentifier}
                        onChange={(e) => setStudentIdentifier(e.target.value)}
                        placeholder="Please specify which fighter you are in the video. For example: Fighter in blue gi"
                        className="mt-1"
                        disabled={isLoading}
                    />
                </div>
                <div>
                    <label htmlFor="martialArt" className="block text-sm font-medium text-foreground">
                        Martial Art
                    </label>
                    <Select
                        value={martialArt}
                        onValueChange={(value) => setMartialArt(value as MartialArt)}
                        disabled={isLoading}
                    >
                        <SelectTrigger className="mt-1">
                            <SelectValue placeholder="Select martial art" />
                        </SelectTrigger>
                        <SelectContent>
                            {martialArtOptions.map((art) => (
                                <SelectItem key={art} value={art}>
                                    {art}
                                </SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>
                <Button
                    type="submit"
                    disabled={!file || isLoading}
                    className="w-full"
                >
                    {isLoading ? 'Uploading...' : `Upload ${uploadType === 'sparring' ? 'Sparring' : 'Demonstration'} Video`}
                </Button>
            </form>
            {isLoading && (
                <div className="mt-4">
                    <progress value={progress} max="100" className="w-full" />
                    <p className="text-center mt-1 text-muted-foreground">{progress}%</p>
                </div>
            )}
            {error && <p className="text-destructive mt-2">{error}</p>}
            {signedUrl && (
                <div className="mt-4">
                    <p className="text-green-500">Upload successful!</p>
                    <video src={signedUrl} controls className="w-full mt-2" />
                </div>
            )}
            {analysisLoading && !analysisCompleted && (
                <div className="mt-4 flex items-center space-x-2">
                    <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-primary"></div>
                    <p className="text-primary">Your video is being analyzed. This may take a few minutes.</p>
                </div>
            )}
            {analysisCompleted && analysisResult && (
                <AiAnalysisResults analysisJson={analysisResult.analysisJson} />
            )}
        </div>
    );
};

export default VideoUploadForm;