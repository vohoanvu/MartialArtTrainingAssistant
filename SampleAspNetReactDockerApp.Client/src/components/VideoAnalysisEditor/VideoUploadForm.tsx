import { useState } from 'react';
import { uploadVideoFile } from '@/services/api';
import { Button } from '../ui/button';
import { VideoUploadResponse, MartialArt } from '@/types/global';
import { Input } from '../ui/input';
import { Textarea } from '../ui/textarea';
import { Select, SelectTrigger, SelectContent, SelectItem, SelectValue } from '../ui/select';
import { useToast } from '@/hooks/use-toast';

interface VideoUploadFormProps {
    fighterRole: number; // 0 for Student, 1 for Instructor
    jwtToken: string;
    hydrateFn: () => Promise<void>;
    isUploading: boolean;
    setIsUploading: (value: boolean) => void;
    onUploadSuccess: (response: VideoUploadResponse) => void;
}

const VideoUploadForm = ({
    jwtToken,
    hydrateFn,
    isUploading,
    setIsUploading,
    onUploadSuccess
}: VideoUploadFormProps) => {
    const [file, setFile] = useState<File | null>(null);
    const [description, setDescription] = useState('');
    const [studentIdentifier, setStudentIdentifier] = useState('');
    const [martialArt, setMartialArt] = useState<MartialArt>(MartialArt.BrazilianJiuJitsu_GI);
    const [signedUrl, setSignedUrl] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [progress, setProgress] = useState(0);
    const { toast } = useToast();

    const title = 'Upload BJJ training video for AI analysis';

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

        setIsUploading(true);
        setError(null);
        setProgress(0);

        try {
            const response: VideoUploadResponse = await uploadVideoFile({
                file,
                description,
                studentIdentifier,
                martialArt,
                jwtToken,
                hydrate: hydrateFn,
                onProgress: (percent: number) => setProgress(percent)
            });
            setFile(null);
            setDescription('');
            setStudentIdentifier('');
            setMartialArt(MartialArt.None);

            if (response.statusCode == 409 && response.message !== null) {
                toast({
                    title: "Duplicate Video Detected",
                    description: response.message,
                    variant: "destructive"
                });
                return;
            }
            setSignedUrl(response.signedUrl || '')
            onUploadSuccess(response);
        } catch (err: any) {
            setError(err);
            toast({
                title: "Upload Failed",
                description: err.message || "An error occurred while uploading the video.",
                variant: "destructive"
            });
            setError(err.message || "An error occurred while uploading the video.");
        } finally {
            setIsUploading(false);
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
                        disabled={isUploading}
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
                        disabled={isUploading}
                    />
                </div>
                <div>
                    <label htmlFor="studentIdentifier" className="block text-sm font-medium text-foreground">
                        Specify which fighter in the video you want the AI to analyze!
                    </label>
                    <Input
                        type="text"
                        id="studentIdentifier"
                        value={studentIdentifier}
                        onChange={(e) => setStudentIdentifier(e.target.value)}
                        placeholder="For example: 'the Fighter in Blue GI, the player is in Black GI'"
                        className="mt-1"
                        disabled={isUploading}
                    />
                </div>
                <div>
                    <label htmlFor="martialArt" className="block text-sm font-medium text-foreground">
                        Martial Art
                    </label>
                    <Select
                        value={martialArt}
                        onValueChange={(value) => setMartialArt(value as MartialArt)}
                        disabled={isUploading}
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
                    disabled={!file || isUploading}
                    className="w-full"
                >
                    {isUploading ? 'Uploading...' : `Upload Video`}
                </Button>
            </form>
            {isUploading && (
                <div className="mt-4">
                    <progress value={progress} max="100" className="w-full" />
                    <p className="text-center mt-1 text-muted-foreground">{progress}%</p>
                </div>
            )}
            {error && <p className="text-destructive mt-2">{error}</p>}
            {signedUrl && (
                <div className="mt-4">
                    <p className="text-green-500">Upload successful!</p>
                    {/* <video src={signedUrl} controls className="w-full mt-2" /> */}
                </div>
            )}
        </div>
    );
};

export default VideoUploadForm;