/**
 * User type
 * @property {number} id - User id
 * @property {string} username - User username
 * @property {string} email - User email
 * @property {boolean} isAdmin - User admin status
 * @property {string} avatarUrl - User avatar url
 */
export type User = {
    id?: number | null;
    username: string | null;
    email: string | null;
    isAdmin: boolean;
    avatarUrl?: string;
}

/**
 * Weather forecast type
 * @property {string || null} date - Date
 * @property {number || null} temperatureC - Temperature in Celsius
 * @property {number || null} temperatureF - Temperature in Fahrenheit
 * @property {string || null} summary - Summary
 */
export type WeatherForecast = {
    /** Format: date */
    date?: string;
    /** Format: int32 */
    temperatureC?: number;
    /** Format: int32 */
    temperatureF?: number;
    summary?: string | null;
};

export interface Fighter {
    id: number;
    fighterName: string;
    belkRank: number;
    role: number;
    birthdate: string; // or Date if you plan to handle it as a Date object
    height: number;
    weight: number;
}

export interface FighterInfo {
    email: string;
    isEmailConfirmed: boolean;
    fighter: Fighter;
}


/**
 * Login request type
 * @property {string} email - User email
 * @property {string} password - User password
 */
export type LoginRequest = {
    email: string;
    password: string;
}


/**
 * TrainingSessionResponse type
 * @property {number} id - training session id
 * @property {string} trainingDate - Date of the training session
 * @property {string} description - Description of the training session
 * @property {number} capacity - Capacity of the training session
 * @property {number} duration - Duration of the training session
 * @property {string} status - Status of the training session
 * @property {number} instructorId - ID of the instructor
 * @property {number[]} studentIds - IDs of the students
 */
export type TrainingSessionResponse = {
    id: number;
    /** Format: date-time */
    trainingDate: string;
    description: string;
    /** Format: int32 */
    capacity: number;
    /** Format: int32 */
    duration: number;
    status: string;
    /** Format: int32 */
    instructorId: number;
    /** Format: int32[] */
    studentIds: number[];
};


export interface SessionDetailViewModel {
    id: number;
    trainingDate: string;
    description: string;
    capacity: number;
    duration: number;
    status: string;
    instructor: FighterViewModel;
    students: FighterViewModel[];
    instructorId: number;
    studentIds: number[];
}

export interface CreateTrainingSessionRequest {
    trainingDate: string;
    description: string;
    capacity: number;
    duration: number;
    status: string;
}

export interface UpdateTrainingSessionRequest {
    trainingDate?: string;
    description?: string;
    capacity?: number;
    duration?: number;
    status?: string;
    instructorId?: number;
    studentIds?: number[];
}

export interface CreateTrainingSessionResponse {
    trainingDate: string;
    description: string;
    capacity: number;
    duration: number;
    status: string;
    instructorId: number;
    studentIds: number[];
}

/**
 * SharedVideo type
 * @property {number || null} id - ID
 * @property {string || null} videoId - Video ID
 * @property {string || null} title - Title
 * @property {string || null} description - Description
 * @property {string || null} embedLink - Embed Link
 * @property {SharedBy || null} sharedBy - Shared By
 */
export type SharedVideo = {
    id?: number;
    videoId?: string;
    title?: string;
    description?: string;
    embedLink?: string;
    sharedBy?: SharedBy | null;
};

/**
 * SharedBy type
 * @property {string || null} userId - User ID
 * @property {string || null} username - Username
 */
export type SharedBy = {
    userId?: string;
    username?: string;
};


export interface FighterViewModel {
    id: number,
    fighterName: string;
    height: number;
    weight: number;
    bmi?: number;
    gender: string;
    birthdate: Date;
    fighterRole: string;
    maxWorkoutDuration: number;
    beltColor: string;
    experience: number;
}

export interface MatchMakerRequest
{
    studentFighterIds: number[]
    instructorFighterId: number
}

export interface FighterPair {
    fighter1: string;
    fighter2: string;
}

export type FighterPairResult = FighterPair[];

export interface GetBMIResponse {
    bmi: string,
    category: string,
    description: string,
}

export interface VideoUploadResponse {
    VideoId?: number; // For sparring
    DemonstrationId?: number; // For demonstration
    SignedUrl: string;
}

export interface VideoDeleteResponse {
    Message: string;
}

// {
//     "bmi": 21.50,
//     "category": "Normal Weight",
//     "description": "You are within the healthy weight range for your height."
// }

