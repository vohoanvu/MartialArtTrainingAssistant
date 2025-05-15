// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-nocheck
import { UploadedVideoDto } from '@/pages/VideoStorageListing';
import { paths } from './endpoints';
import {
    CreateTrainingSessionRequest,
    CreateTrainingSessionResponse,
    TrainingSessionResponse,
    SharedVideo,
    SessionDetailViewModel,
    MatchMakerRequest,
    FighterPairResult,
    GetBMIResponse,
    VideoUploadResponse,
    VideoDeleteResponse,
    MartialArt,
    FighterInfo,
    Fighter,
    AnalysisResultDto,
    CurriculumDto,
} from "@/types/global.ts";
import axios from 'axios';

type Path = keyof paths;
type PathMethod<T extends Path> = keyof paths[T];

type RequestParams<P extends Path, M extends PathMethod<P>> = paths[P][M] extends {
    parameters: unknown;
}
    ? paths[P][M]['parameters']
    : undefined;
type ResponseType<P extends Path, M extends PathMethod<P>> = paths[P][M] extends {
    responses: { 200: { schema: { [x: string]: unknown } } };
}
    ? paths[P][M]['responses'][200]['schema']
    : undefined;

export const apiCall = <P extends Path, M extends PathMethod<P>>(
    url: P,
    method: M,
    ...params: RequestParams<P, M> extends undefined ? [] : [RequestParams<P, M>]
): Promise<ResponseType<P, M>> => {
    console.log("Inspecting url: ", url);
    console.log("Inspecting method: ", method)
    console.log("Inspecting params: ", params);
};

export async function getTrainingSessions({ currentTry = 0, jwtToken, refreshToken, hydrate }): Promise<TrainingSessionResponse[]> {
    console.log("Trying to get training session list...");

    const response = await fetch("api/trainingsession", {
        headers: {
            'Content-Type': 'application/json',
            //'Authorization': `Bearer ${jwtToken}`
        }
    });
    if (response.ok) {
        console.log("Training sessions fetched successfully!");
        return await response.json() as TrainingSessionResponse[];
    } else if (response.status === 401 && currentTry === 0) {
        await hydrate();
        console.log("Refresh token and try again...", refreshToken);
        return await getTrainingSessions({ currentTry: 1, jwtToken, refreshToken, hydrate });
    }

    throw new Error("Error fetching training sessions");
}

export async function createTrainingSession(newSession: CreateTrainingSessionRequest, jwtToken: string): Promise<CreateTrainingSessionResponse> {
    console.log("Do we have jwt token here?...", jwtToken)
    try {
        const response = await fetch('/api/trainingsession', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                //'Authorization': `Bearer ${jwtToken}`,
            },
            body: JSON.stringify(newSession),
        });

        if (!response.ok) {
            throw new Error(`Failed to create a new training session: ${response.statusText}`);
        }

        return await response.json() as CreateTrainingSessionResponse;
    } catch (error) {
        console.error('Error creating new training session:', error);
        throw error;
    }
}

export async function getAllSharedVideos({ currentTry = 0, jwtToken, refreshToken, hydrate }): Promise<SharedVideo[]> {
    console.log("Fetching shared videos from Microservice D...");
    const response = await fetch("/vid/api/video/getall", {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${jwtToken}`
        }
    });

    if (response.ok) {
        console.log("Shared videos fetched successfully!");
        return await response.json() as SharedVideo[];
    } else if (response.status === 401 && currentTry === 0) {
        await hydrate();
        return await getAllSharedVideos({ currentTry: 1, jwtToken, refreshToken, hydrate });
    }

    throw new Error("Error fetching shared videos");
}

export async function uploadYoutubeVideo({
    videoUrl,
    jwtToken,
    currentTry = 0,
    hydrate
}): Promise<SharedVideo> {
    console.log("Uploading YouTube video metadata onto Microservice D...");
    console.log("Bearer token is: ", jwtToken);
    const response = await fetch(`/vid/api/video/metadata`, {
        method: 'POST',
        body: JSON.stringify({
            videoUrl: encodeURIComponent(videoUrl)
        }),
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${jwtToken}`
        }
    });

    if (response.ok) {
        console.log("YouTube video metadata uploaded successfully!");
        return await response.json() as SharedVideo;
    } else if (response.status === 401 && currentTry === 0) {
        await hydrate();
        return await uploadYoutubeVideo({ videoUrl, jwtToken, currentTry: 1, hydrate });
    } else {
        const errorText = await response.text();
        throw new Error(`Error uploading YouTube video metadata: ${errorText}`);
    }
}

export async function getAuthenticatedFighterInfo({ currentTry = 0, jwtToken, refreshToken, hydrate }): Promise<FighterInfo> {
    console.log("Fetching Fighter Info details...");

    const response = await fetch(`/api/fighter/info`, {
        method: "GET",
        headers: {
            'Content-Type': 'application/json',
            //'Authorization': `Bearer ${jwtToken}`
        }
    });
    console.log("JWT beaker token: ", jwtToken);
    if (response.ok) {
        console.log("Fighter Info details fetched successfully!");
        const data = await response.json();
        console.log(data);
        return data as FighterInfo;
    } else if (response.status === 401 && currentTry === 0) {
        await hydrate();
        console.log("Refresh token and try again...", refreshToken);
        return await getAuthenticatedFighterInfo({ currentTry: 1, jwtToken, refreshToken, hydrate });
    }

    throw new Error("Error fetching Info details details");
}

export async function getTrainingSessionDetails(sessionId: number, { currentTry = 0, jwtToken, refreshToken, hydrate }): Promise<SessionDetailViewModel> {
    console.log("Trying to get training session details...");

    const response = await fetch(`/api/trainingsession/${sessionId}`, {
        method: "GET",
        headers: {
            'Content-Type': 'application/json',
            //'Authorization': `Bearer ${jwtToken}`
        }
    });
    if (response.ok) {
        const data = await response.json();
        console.log("Training session details fetched successfully!");
        console.log("Inspecting response: ", data);
        return data as SessionDetailViewModel;
    } else if (response.status === 401 && currentTry === 0) {
        await hydrate();
        console.log("Refresh token and try again...", refreshToken);
        return await getTrainingSessionDetails(sessionId, { currentTry: 1, jwtToken, refreshToken, hydrate });
    }

    throw new Error("Error fetching training session details");
}

export async function updateTrainingSessionDetails(sessionId: number, updateRequest: UpdateTrainingSessionRequest, { currentTry = 0, jwtToken, refreshToken, hydrate }): Promise<SessionDetailViewModel> {
    console.log("Updating training session details from Microservice A...", updateRequest);

    const response = await fetch(`/api/trainingsession/${sessionId}`, {
        method: "PUT",
        body: JSON.stringify(updateRequest),
        headers: {
            'Content-Type': 'application/json',
            //'Authorization': `Bearer ${jwtToken}`
        }
    });
    if (response.ok) {
        console.log("Training session details fetched successfully!");
        return await response.json() as SessionDetailViewModel;
    } else if (response.status === 401 && currentTry === 0) {
        await hydrate();
        console.log("Refresh token and try again...", refreshToken);
        return await updateTrainingSessionDetails({ currentTry: 1, jwtToken, refreshToken, hydrate });
    }

    throw new Error("Error fetching training session details");
}

export async function GenerateFighterPairs(matchMakerRequest: MatchMakerRequest, {
    jwtToken,
    currentTry = 0,
    hydrate
}): Promise<FighterPairResult> {
    console.log("Generating Fighter Pairs from Microservice C...");

    const response = await fetch(`/pair/api/matchmaker`, {
        method: 'POST',
        body: JSON.stringify(matchMakerRequest),
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${jwtToken}`
        }
    });

    if (response.ok) {
        console.log("Succcesfully generating Pair!");
        return await response.json() as FighterPairResult;
    } else if (response.status === 401 && currentTry === 0) {
        await hydrate();
        return await GenerateFighterPairs({ videoUrl, jwtToken, currentTry: 1, hydrate });
    } else {
        const errorText = await response.text();
        throw new Error(`Error generating fighter pairs: ${errorText}`);
    }
}

export async function CalculateBMI(height: number, weight: number): Promise<GetBMIResponse> {
    console.log("Calculating BMI from microservice A...");

    try {
        const response = await fetch('http://localhost:1111/calculate-bmi', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ height, weight }),
        });

        return await response.json();
    } catch (error) {
        throw new Error(`Failed to call Microservice A: ${error}`);
    }
}


interface UploadVideoParams {
    file: File;
    description: string;
    studentIdentifier: string;
    martialArt: MartialArt;
    uploadType: 'sparring' | 'demonstration';
    jwtToken: string;
    currentTry?: number;
    hydrate: () => Promise<void>;
    onProgress?: (percent: number) => void;
}
export async function uploadVideoFile({
    file,
    description,
    studentIdentifier,
    martialArt,
    uploadType,
    jwtToken,
    currentTry = 0,
    hydrate,
    onProgress,
}: UploadVideoParams): Promise<VideoUploadResponse> {
    console.log(`Uploading ${uploadType} video to Microservice D...`);

    const formData = new FormData();
    formData.append('videoFile', file);
    formData.append('description', description);
    formData.append('studentIdentifier', studentIdentifier);
    formData.append('martialArt', martialArt);

    const endpoint = uploadType === 'sparring'
        ? '/vid/api/video/upload-sparring'
        : '/vid/api/video/upload-demonstration';

    try {
        const response = await axios.post(endpoint, formData, {
            headers: {
                'Authorization': `Bearer ${jwtToken}`,
            },
            onUploadProgress: (progressEvent: ProgressEvent) => {
                const percentCompleted = Math.round(
                    (progressEvent.loaded * 100) / progressEvent.total
                );
                console.log(`Upload progress: ${percentCompleted}%`);
                if (onProgress) onProgress(percentCompleted);
            },
        });

        console.log(`${uploadType} video uploaded successfully!`);
        return response.data as VideoUploadResponse;
    } catch (error: any) {
        if (error.response?.status === 409) {
            console.warn('Duplicate video detected:', error.response.data.Message);
            return Promise.reject({
                message: error.response.data.Message,
                signedUrl: error.response.data.SignedUrl,
                videoId: error.response.data.VideoId,
            });
        }

        if (error.response?.status === 401 && currentTry === 0) {
            await hydrate();
            return await uploadVideoFile({
                file,
                description,
                studentIdentifier,
                martialArt,
                uploadType,
                jwtToken,
                currentTry: 1,
                hydrate,
                onProgress,
            });
        }

        const errorText = error.response?.data || error.response?.statusText || error.message;
        throw new Error(`Error uploading ${uploadType} video: ${errorText}`);
    }
}


export async function deleteUploadedVideo({
    videoId,
    jwtToken,
    currentTry = 0,
    hydrate,
}: {
    videoId: number;
    jwtToken: string | null;
    currentTry?: number;
    hydrate: () => Promise<void>;
}): Promise<VideoDeleteResponse> {
    console.log(`Deleting uploaded video with ID ${videoId}...`);

    const response = await fetch(`/vid/api/video/delete-uploaded/${videoId}`, {
        method: 'DELETE',
        headers: {
            'Authorization': `Bearer ${jwtToken}`,
        },
    });

    if (response.ok) {
        console.log(`Video with ID ${videoId} deleted successfully!`);
        return await response.json() as VideoDeleteResponse;
    } else if (response.status === 401 && currentTry === 0) {
        await hydrate();
        return await deleteUploadedVideo({ videoId, jwtToken, currentTry: 1, hydrate });
    } else {
        const errorText = await response.text();
        throw new Error(`Error deleting video: ${errorText}`);
    }
}

export async function getUploadedVideos({
    jwtToken,
    refreshToken,
    hydrate,
    currentTry = 0,
}: {
    jwtToken: string | null;
    refreshToken: string | null;
    hydrate: () => Promise<void>;
    currentTry?: number;
}): Promise<UploadedVideoDto[]> {
    console.log("Fetching uploaded videos...");

    try {
        const response = await fetch('/vid/api/video/getall-uploaded', {
            headers: {
                'Authorization': `Bearer ${jwtToken}`,
            },
        });

        if (response.ok) {
            console.log("Uploaded videos fetched successfully!");
            return await response.json() as UploadedVideoDto[];
        } else if (response.status === 401 && currentTry === 0) {
            console.log("Access token expired. Attempting to refresh...");
            await hydrate();
            return await getUploadedVideos({ jwtToken, refreshToken, hydrate, currentTry: 1 });
        } else {
            const errorText = await response.text();
            throw new Error(`Error fetching uploaded videos: ${errorText}`);
        }
    } catch (error) {
        console.error("Error fetching uploaded videos:", error);
        throw error;
    }
}

export async function getVideoDetails({
    videoId,
    jwtToken,
    refreshToken,
    hydrate,
    currentTry = 0,
}: {
    videoId: string;
    jwtToken: string | null;
    refreshToken: string | null;
    hydrate: () => Promise<void>;
    currentTry?: number;
}): Promise<UploadedVideoDto> {
    try {
        const response = await fetch(`/vid/api/video/${videoId}`, {
            headers: {
                Authorization: `Bearer ${jwtToken}`,
            },
        });

        if (response.ok) {
            return await response.json();
        } else if (response.status === 401 && currentTry === 0) {
            await hydrate();
            return await getVideoDetails({ videoId, jwtToken, refreshToken, hydrate, currentTry: 1 });
        } else {
            throw new Error(`Error fetching video details: ${response.statusText}`);
        }
    } catch (error) {
        console.error("Error fetching video details:", error);
        throw error;
    }
}

export async function getVideoFeedback({
    videoId,
    jwtToken,
    refreshToken,
    hydrate,
    currentTry = 0,
}: {
    videoId: string;
    jwtToken: string | null;
    refreshToken: string | null;
    hydrate: () => Promise<void>;
    currentTry?: number;
}): Promise<AnalysisResultDto> {
    try {
        const response = await fetch(`/vid/api/video/${videoId}/feedback`, {
            headers: {
                Authorization: `Bearer ${jwtToken}`,
            },
        });

        if (response.ok) {
            return await response.json();
        } else if (response.status === 401 && currentTry === 0) {
            await hydrate();
            return await getVideoFeedback({ videoId, jwtToken, refreshToken, hydrate, currentTry: 1 });
        } else {
            throw new Error(`Error fetching video feedback: ${response.statusText}`);
        }
    } catch (error) {
        console.error("Error fetching video feedback:", error);
        throw error;
    }
}

export async function saveVideoAnalysisResult({
    videoId,
    analysisResultBody,
    jwtToken,
    refreshToken,
    hydrate,
}: {
    videoId: string;
    analysisResultBody: AnalysisResultDto;
    jwtToken: string | null;
    refreshToken: string | null;
    hydrate: () => Promise<void>;
}): Promise<AnalysisResultDto> {
    const response = await fetch(`/vid/api/video/${videoId}/analysis`, {
        method: 'PATCH',
        headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${jwtToken}`,
        },
        body: JSON.stringify(analysisResultBody),
    });

    if (response.status === 401) {
        await hydrate();
        const retryResponse = await fetch(`/api/video/${videoId}/analysis`, {
            method: 'PATCH',
            headers: {
                'Content-Type': 'application/json',
                Authorization: `Bearer ${jwtToken}`,
            },
            body: JSON.stringify(analysisResultBody),
        });

        if (!retryResponse.ok) {
            throw new Error(`Failed to save analysis result: ${retryResponse.statusText}`);
        }

        return await retryResponse.json();
    }

    if (!response.ok) {
        throw new Error(`Failed to save analysis result: ${response.statusText}`);
    }

    return await response.json();
}


export async function getFighterDetails({
    fighterId,
    jwtToken,
    refreshToken,
    hydrate,
    currentTry = 0,
}: {
    fighterId: number;
    jwtToken: string | null;
    refreshToken: string | null;
    hydrate: () => Promise<void>;
    currentTry?: number;
}): Promise<Fighter> {
    try {
        console.log("Fetching fighter details for fighterId:", fighterId);
        console.log("JWT Token:", jwtToken);

        //authentication via Identity Cookie
        const response = await fetch(`/api/fighter/${fighterId}`, {
            method: 'GET',
        });

        if (response.ok) {
            const data = await response.json();
            console.log("Fighter details fetched successfully:", data);
            return data as Fighter;
        } else if (response.status === 401 && currentTry === 0) {
            console.log("Token expired or unauthorized. Refreshing token...");
            await hydrate();
            return await getFighterDetails({ fighterId, jwtToken, refreshToken, hydrate, currentTry: 1 });
        } else {
            const errorText = await response.text();
            throw new Error(`Error fetching fighter details: ${response.statusText} - ${errorText}`);
        }
    } catch (error) {
        console.error("Error fetching fighter details:", error);
        throw error;
    }
}

export async function generateClassCurriculum({
    sessionId,
    jwtToken,
    refreshToken,
    hydrate,
    currentTry = 0,
}: {
    sessionId: number;
    jwtToken: string | null;
    refreshToken: string | null;
    hydrate: () => Promise<void>;
    currentTry?: number;
}): Promise<CurriculumDto> {
    try {
        const response = await fetch(`/vid/api/video/session/${sessionId}/generate`, {
            headers: {
                'Authorization': `Bearer ${jwtToken}`,
            },
        });

        if (response.ok) {
            return await response.json() as CurriculumDto;
        } else if (response.status === 401 && currentTry === 0) {
            await hydrate();
            return await generateClassCurriculum({ sessionId, jwtToken, refreshToken, hydrate, currentTry: 1 });
        } else {
            const errorText = await response.text();
            throw new Error(`Failed to fetch curriculum: ${errorText}`);
        }
    } catch (error) {
        console.error("Error fetching curriculum:", error);
        throw error;
    }
}

export async function getClassCurriculum({
    sessionId,
    jwtToken,
    refreshToken,
    hydrate,
    currentTry = 0,
}: {
    sessionId: number;
    jwtToken: string | null;
    refreshToken: string | null;
    hydrate: () => Promise<void>;
    currentTry?: number;
}): Promise<CurriculumDto | null> {
    try {
        const response = await fetch(`/vid/api/video/${sessionId}/curriculum`, {
            headers: {
                'Authorization': `Bearer ${jwtToken}`,
            },
        });

        if (response.ok && response.status === 204) {
            console.log("Get Curriculum response: ", response);
            return null;
        }

        if (response.ok) {
            return await response.json() as CurriculumDto;
        } else if (response.status === 401 && currentTry === 0) {
            await hydrate();
            return await getClassCurriculum({ sessionId, jwtToken, refreshToken, hydrate, currentTry: 1 });
        } else {
            const errorText = await response.text();
            throw new Error(`Failed to fetch curriculum: ${errorText}`);
        }
    } catch (error) {
        console.error("Error fetching curriculum:", error);
        throw error;
    }
}

export async function joinWailList({ email, role, region }: 
{ email: string; role?: string; region?: string }) 
{
    try {
        const response = await fetch('/api/fighter/join-waitlist', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                email,
                role,
                region,
            }),
        });

        if (response.ok) {
            return await response.json();
        } else {
            const errorText = await response.text();
            throw new Error(`Failed to join waitlist: ${errorText}`);
        }
    } catch (error) {
        console.error("Error joining waitlist:", error);
        throw error;
    }
}