import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import ConfirmationDialog from '@/components/ui/ConfirmationDialog';
import { Button } from '@/components/ui/button';
import { CreateTrainingSessionRequest, UpdateTrainingSessionRequest } from '@/types/global';
import { createTrainingSession, updateTrainingSessionDetails , getTrainingSessionDetails, deleteTrainingSession, closeTrainingSession } from '@/services/api';
import useAuthStore from '@/store/authStore';

export type TargetLevel = 'Kids' | 'Beginner' | 'Intermediate' | 'Advanced' | 'Expert';
const TARGET_LEVEL_OPTIONS: TargetLevel[] = [
    'Kids',
    'Beginner',
    'Intermediate',
    'Advanced',
    'Expert'
];

const TrainingSessionForm = () => {
    const { sessionId  } = useParams<{ sessionId?: string }>();
    const sessionIdNumber = sessionId ? Number(sessionId) : null;
    const navigate = useNavigate();
    const jwtToken = useAuthStore((state) => state.accessToken);
    const refreshToken = useAuthStore((state) => state.refreshToken);
    const hydrate = useAuthStore((state) => state.hydrate);
    const user = useAuthStore((state) => state.user);

    const [instructorName, setInstructorName] = useState('');
    const [trainingDate, setTrainingDate] = useState('');
    const [capacity, setCapacity] = useState('');
    const [duration, setDuration] = useState('');
    const [description, setDescription] = useState('');
    const [isDialogOpen, setIsDialogOpen] = useState(false);
    const [targetLevel, setTargetLevel] = useState<TargetLevel>('Beginner');
    const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
    const [isClosing, setIsClosing] = useState(false);
    const [isDeleting, setIsDeleting] = useState(false);

    useEffect(() => {
        if (sessionIdNumber) {
            const fetchSessionDetails = async () => {
                try {
                    const details = await getTrainingSessionDetails(sessionIdNumber, { jwtToken, refreshToken, hydrate });
                    setInstructorName(details.instructor.fighterName);
                    setTrainingDate(details.trainingDate.slice(0, 16));
                    setCapacity(details.capacity.toString());
                    setDuration(parseHoursToMinutesString(details.duration));
                    setDescription(details.description ?? '');
                    setTargetLevel(details.targetLevel as TargetLevel);
                } catch (error) {
                    console.error("Failed to load session details:", error);
                }
            };
            fetchSessionDetails();
        }
    }, [hydrate, jwtToken, refreshToken, sessionIdNumber])

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        setIsDialogOpen(true);
    };

    function parseMinutesToHoursFloat(minutes: string): number {
        const parsed = parseFloat(minutes.replace(',', '.'));
        if (isNaN(parsed) || parsed <= 0) return 0;
        return parsed / 60;
    }
    function parseHoursToMinutesString(hours: number): string {
        if (isNaN(hours) || hours <= 0) return "0";
        return Math.round(hours * 60).toString();
    }

    const handleConfirm = async () => {
        if (!sessionIdNumber) {
            const newSession: CreateTrainingSessionRequest = {
                trainingDate,
                description,
                capacity: parseInt(capacity, 10),
                duration: parseMinutesToHoursFloat(duration),
                status: 'Active',
                targetLevel: targetLevel,
            };
            try {
                await createTrainingSession(newSession, jwtToken!);
                setIsDialogOpen(false);
                navigate('/class-session');
            } catch (error) {
                console.error("Failed to create a new session:", error);
            }
        } else {
            const updatedSession: UpdateTrainingSessionRequest = {
                trainingDate,
                description,
                capacity: parseInt(capacity, 10),
                duration: parseMinutesToHoursFloat(duration),
                status: 'Active',
                targetLevel: targetLevel,
            };
            try {
                await updateTrainingSessionDetails(sessionIdNumber, updatedSession, { jwtToken, refreshToken, hydrate });
                setIsDialogOpen(false);
                navigate('/class-session');
            } catch (error) {
                console.error("Failed to update the session:", error);
            }
        }
    };

    const handleCancel = () => {
        setIsDialogOpen(false);
    };

    const handleCloseSession = async () => {
        try {
            setIsClosing(true);
            await closeTrainingSession(sessionIdNumber!, { jwtToken, refreshToken, hydrate });
            // Refresh the session details or navigate away
            navigate('/class-session');
        } catch (error) {
            console.error('Failed to close session:', error);
            // Handle error (show toast/alert)
        } finally {
            setIsClosing(false);
        }
    };

    const handleDeleteSession = async () => {
        try {
            setIsDeleting(true);
            await deleteTrainingSession(sessionIdNumber!, { jwtToken, refreshToken, hydrate });
            navigate('/class-session');
        } catch (error) {
            console.error('Failed to delete session:', error);
            // Handle error (show toast/alert)
        } finally {
            setIsDeleting(false);
            setIsDeleteDialogOpen(false);
        }
    };

    return (
        <div className="container mx-auto max-w-lg p-8 shadow-lg rounded-lg">
            <h1 className="text-3xl font-bold mb-6">
                {sessionId ? 'Update Session Details' : 'Create New Session'}
            </h1>
            <form onSubmit={handleSubmit} className="space-y-4">
                {/* Instructor Name field */}
                <div>
                    <label htmlFor="instructorName" className="block text-sm font-medium">
                        Instructor Name
                    </label>
                    <input
                        type="text"
                        id="instructorName"
                        value={instructorName}
                        onChange={(e) => setInstructorName(e.target.value)}
                        className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
                        placeholder="Enter instructor name"
                        required
                    />
                </div>

                {/* Training DateTime field */}
                <div>
                    <label htmlFor="trainingDate" className="block text-sm font-medium">
                        Training Date
                    </label>
                    <input
                        type="datetime-local"
                        id="trainingDate"
                        value={trainingDate}
                        onChange={(e) => setTrainingDate(e.target.value)}
                        className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
                        required
                    />
                </div>

                {/* Capacity field */}
                <div>
                    <label htmlFor="capacity" className="block text-sm font-medium">
                        Capacity
                    </label>
                    <div className="flex">
                        <input
                            type="number"
                            id="capacity"
                            value={capacity}
                            onChange={(e) => setCapacity(e.target.value)}
                            className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
                            placeholder="Enter capacity"
                            required
                        />
                        <span className="ml-2 mt-3">people</span>
                    </div>
                </div>

                {/* Duration field */}
                <div>
                    <label htmlFor="duration" className="block text-sm font-medium">
                        Duration
                    </label>
                    <div className="flex">
                        <input
                            type="number"
                            id="duration"
                            value={duration}
                            onChange={(e) => setDuration(e.target.value)}
                            className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
                            placeholder="Enter duration"
                            required
                        />
                        <span className="ml-2 mt-3">minutes</span>
                    </div>
                </div>

                {/* <div>
                    <label htmlFor="status" className="block text-sm font-medium">
                        Status
                    </label>
                    <select
                        id="status"
                        value={status}
                        onChange={(e) => setStatus(e.target.value)}
                        className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
                        required
                    >
                        <option value="" disabled>
                            Select status
                        </option>
                        <option value="Active">Active</option>
                        <option value="Completed">Completed</option>
                        <option value="Cancelled">Cancelled</option>
                    </select>
                </div> */}

                {/* Target Level field */}
                <div>
                    <label htmlFor="targetLevel" className="block text-sm font-medium">
                        Target Level
                    </label>
                    <select
                        id="targetLevel"
                        value={targetLevel}
                        onChange={(e) => setTargetLevel(e.target.value as TargetLevel)}
                        className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
                        required
                    >
                        {TARGET_LEVEL_OPTIONS.map((level) => (
                            <option key={level} value={level}>
                                {level}
                            </option>
                        ))}
                    </select>
                </div>

                {/* Description field */}
                <div>
                    <label htmlFor="description" className="block text-sm font-medium">
                        Description Notes
                    </label>
                    <textarea
                        id="description"
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
                        placeholder="Enter description notes"
                    ></textarea>
                </div>

                <Button type="submit" className="w-full">
                    {sessionId ? 'Update' : 'Create'}
                </Button>
            </form>

            {/* Add these buttons in the instructor section */}
            {user && user.fighterInfo?.role !== 0 && (
                <div className="flex gap-2 mt-4">
                    <Button
                        type="button"
                        variant="secondary"
                        onClick={handleCloseSession}
                        disabled={isClosing}
                    >
                        {isClosing ? (
                            <>
                                <div className="animate-spin h-4 w-4 mr-2 border-t-2 border-b-2 border-current"></div>
                                Closing...
                            </>
                        ) : (
                            'Close Session'
                        )}
                    </Button>
                    <Button
                        type="button"
                        variant="destructive"
                        onClick={() => setIsDeleteDialogOpen(true)}
                        disabled={isDeleting}
                    >
                        {isDeleting ? (
                            <>
                                <div className="animate-spin h-4 w-4 mr-2 border-t-2 border-b-2 border-current"></div>
                                Deleting...
                            </>
                        ) : (
                            'Delete Session'
                        )}
                    </Button>
                </div>
            )}

            <ConfirmationDialog
                title={`Are you sure you want to ${sessionId ? 'update' : 'create'} a new session?`}
                message="You can always change your session details later!"
                isOpen={isDialogOpen}
                onConfirm={handleConfirm}
                onCancel={handleCancel}
            />
            <ConfirmationDialog
                title="Delete Training Session"
                message="Are you sure you want to delete this training session? This action cannot be undone."
                isOpen={isDeleteDialogOpen}
                onConfirm={handleDeleteSession}
                onCancel={() => setIsDeleteDialogOpen(false)}
            />
        </div>
    );
};

export default TrainingSessionForm;