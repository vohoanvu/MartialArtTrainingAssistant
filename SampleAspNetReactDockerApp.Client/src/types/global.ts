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


