// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-nocheck

import { paths } from './endpoints';
//import useAuthStore from "@/store/authStore.ts";
import { WeatherForecast, SharedVideo} from "@/types/global.ts";

type Path = keyof paths;
type PathMethod<T extends Path> = keyof paths[T];

type RequestParams<P extends Path, M extends PathMethod<P>> = paths[P][M] extends {
        parameters: any;
    }
    ? paths[P][M]['parameters']
    : undefined;
type ResponseType<P extends Path, M extends PathMethod<P>> = paths[P][M] extends {
        responses: { 200: { schema: { [x: string]: any } } };
    }
    ? paths[P][M]['responses'][200]['schema']
    : undefined;

export const apiCall = <P extends Path, M extends PathMethod<P>>(
    url: P,
    method: M,
    ...params: RequestParams<P, M> extends undefined ? [] : [RequestParams<P, M>]
): Promise<ResponseType<P, M>> => {
};

export async function getWeatherForecast({currentTry = 0, jwtToken, refreshToken, hydrate}) : Promise<WeatherForecast[]> {
    console.log("Test1");
    
    console.log("Trying to get weather forecast...");
    const response = await fetch("api/v1/weatherforecast", {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${jwtToken}`
        }
    });
    
    if(response.ok) {
        console.log("Weather forecast fetched successfully!");
        return await response.json() as WeatherForecast[];
    }
    else if(response.status === 401 && currentTry === 0) {
        await hydrate();
        return await getWeatherForecast({currentTry : 1});
    }
    
    throw new Error("Error fetching weather forecast");
}

export async function getAllSharedVideos({currentTry = 0, jwtToken, refreshToken, hydrate}): Promise<SharedVideo[]> {
    console.log("Trying to fetch all shared videos...");
    const response = await fetch("/api/video/getall", {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${jwtToken}`
        }
    });

    if(response.ok) {
        console.log("Shared videos fetched successfully!");
        return await response.json() as SharedVideo[];
    } else if(response.status === 401 && currentTry === 0) {
        await hydrate();
        return await getAllSharedVideos({currentTry: 1, jwtToken, refreshToken, hydrate});
    }

    throw new Error("Error fetching shared videos");
}

export async function uploadYoutubeVideo({
    videoUrl,
    jwtToken,
    currentTry = 0,
    hydrate
}): Promise<SharedVideo> 
{
    console.log("Trying to upload YouTube video metadata...");

    const response = await fetch(`/api/video/metadata/${encodeURIComponent(videoUrl)}`, {
        method: 'POST',
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
  