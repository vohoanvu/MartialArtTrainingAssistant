import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import ConfirmationDialog from '@/components/ui/ConfirmationDialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import {
    Select,
    SelectTrigger,
    SelectContent,
    SelectItem,
    SelectValue,
} from '@/components/ui/select';
import {
    CreateTrainingSessionRequest,
    SessionDetailViewModel,
    UpdateTrainingSessionRequest,
} from '@/types/global';
import {
    createTrainingSession,
    updateTrainingSessionDetails,
    getTrainingSessionDetails,
    deleteTrainingSession,
    closeTrainingSession,
} from '@/services/api';
import useAuthStore from '@/store/authStore';

export type TargetLevel = 'Kids' | 'Beginner' | 'Intermediate' | 'Advanced' | 'Expert';
const TARGET_LEVEL_OPTIONS: TargetLevel[] = [
    'Kids',
    'Beginner',
    'Intermediate',
    'Advanced',
    'Expert',
];

const emptySession: SessionDetailViewModel = {
    id: 0,
    instructor: { 
        id: 0,
        fighterName: '',
        height: 0,
        weight: 0,
        gender: '',    
        birthdate: new Date(),
        fighterRole: '',
        maxWorkoutDuration: 0,
        beltColor: '',
        experience: 0,
    },
    instructorId: 0,
    students: [],
    studentIds: [],
    trainingDate: '',
    capacity: 0,
    duration: 0,
    description: '',
    status: 'Active',
    targetLevel: 'Beginner',
    isCurriculumGenerated: false,
};

const TrainingSessionForm = () => {
    const { sessionId } = useParams<{ sessionId?: string }>();
    const sessionIdNumber = sessionId ? Number(sessionId) : null;
    const navigate = useNavigate();
    const jwtToken = useAuthStore((state) => state.accessToken);
    const refreshToken = useAuthStore((state) => state.refreshToken);
    const hydrate = useAuthStore((state) => state.hydrate);
    const user = useAuthStore((state) => state.user);

    const [session, setSession] = useState<SessionDetailViewModel>(emptySession);
    const [isDialogOpen, setIsDialogOpen] = useState(false);
    const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
    const [isClosing, setIsClosing] = useState(false);
    const [isDeleting, setIsDeleting] = useState(false);

    useEffect(() => {
        if (sessionIdNumber) {
            const fetchSessionDetails = async () => {
                try {
                    const details: SessionDetailViewModel = await getTrainingSessionDetails(
                        sessionIdNumber,
                        { jwtToken, refreshToken, hydrate }
                    );
                    setSession({
                        ...details,
                        trainingDate: details.trainingDate.slice(0, 16),
                        capacity: details.capacity,
                        duration: Math.round(details.duration * 60), // store as minutes for input
                        description: details.description ?? '',
                        targetLevel: details.targetLevel as TargetLevel,
                    });
                    console.log("Session Status: ", details.status);
                } catch (error) {
                    console.error('Failed to load session details:', error);
                }
            };
            fetchSessionDetails();
        } else {
            setSession({
                ...emptySession,
            });
        }
    }, [hydrate, jwtToken, refreshToken, sessionIdNumber, user]);

    const handleChange = (field: keyof SessionDetailViewModel, value: any) => {
        setSession((prev) => ({
            ...prev,
            [field]: value,
        }));
    };

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        setIsDialogOpen(true);
    };

    const handleConfirm = async () => {
        if (!sessionIdNumber) {
            const newSession: CreateTrainingSessionRequest = {
                trainingDate: session.trainingDate,
                description: session.description,
                capacity: Number(session.capacity),
                duration: Number(session.duration) / 60,
                status: 'Active',
                targetLevel: session.targetLevel,
            };
            try {
                await createTrainingSession(newSession, jwtToken!);
                setIsDialogOpen(false);
                navigate('/class-session');
            } catch (error) {
                console.error('Failed to create a new session:', error);
            }
        } else {
            const updatedSession: UpdateTrainingSessionRequest = {
                trainingDate: session.trainingDate,
                description: session.description,
                capacity: Number(session.capacity),
                duration: Number(session.duration) / 60,
                status: session.status,
                targetLevel: session.targetLevel,
            };
            try {
                await updateTrainingSessionDetails(sessionIdNumber, updatedSession, {
                    jwtToken,
                    refreshToken,
                    hydrate,
                });
                setIsDialogOpen(false);
                navigate('/class-session');
            } catch (error) {
                console.error('Failed to update the session:', error);
            }
        }
    };

    const handleCancel = () => setIsDialogOpen(false);

    const handleCloseSession = async () => {
        try {
            setIsClosing(true);
            await closeTrainingSession(sessionIdNumber!, { jwtToken, refreshToken, hydrate });
            navigate('/class-session');
        } catch (error) {
            console.error('Failed to close session:', error);
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
                {/* Instructor Name */}
                <div>
                    <label htmlFor="instructorName" className="block text-sm font-medium">
                        Instructor Name
                    </label>
                    <Input
                        id="instructorName"
                        value={!sessionId ? user?.fighterInfo?.fighterName : session.instructor.fighterName}
                        onChange={(e) =>
                            setSession((prev) => ({
                                ...prev,
                                instructor: { ...prev.instructor, fighterName: e.target.value },
                            }))
                        }
                        placeholder="Enter instructor name"
                        required
                        disabled={true}
                    />
                </div>

                {/* Training DateTime */}
                <div>
                    <label htmlFor="trainingDate" className="block text-sm font-medium">
                        Training Date
                    </label>
                    <Input
                        id="trainingDate"
                        type="datetime-local"
                        value={session.trainingDate}
                        onChange={(e) => handleChange('trainingDate', e.target.value)}
                        required
                    />
                </div>

                {/* Capacity */}
                <div>
                    <label htmlFor="capacity" className="block text-sm font-medium">
                        Capacity
                    </label>
                    <div className="flex">
                        <Input
                            id="capacity"
                            type="number"
                            value={session.capacity}
                            onChange={(e) => handleChange('capacity', e.target.value)}
                            placeholder="Enter capacity"
                            required
                        />
                        <span className="ml-2 mt-3">people</span>
                    </div>
                </div>

                {/* Duration */}
                <div>
                    <label htmlFor="duration" className="block text-sm font-medium">
                        Duration
                    </label>
                    <div className="flex">
                        <Input
                            id="duration"
                            type="number"
                            value={session.duration}
                            onChange={(e) => handleChange('duration', e.target.value)}
                            placeholder="Enter duration"
                            required
                        />
                        <span className="ml-2 mt-3">minutes</span>
                    </div>
                </div>

                {/* Target Level */}
                <div>
                    <label htmlFor="targetLevel" className="block text-sm font-medium">
                        Target Level
                    </label>
                    <Select
                        value={session.targetLevel}
                        onValueChange={(value) => handleChange('targetLevel', value as TargetLevel)}
                    >
                        <SelectTrigger id="targetLevel">
                            <SelectValue placeholder="Select level" />
                        </SelectTrigger>
                        <SelectContent>
                            {TARGET_LEVEL_OPTIONS.map((level) => (
                                <SelectItem key={level} value={level}>
                                    {level}
                                </SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>

                {/* Description */}
                <div>
                    <label htmlFor="description" className="block text-sm font-medium">
                        Description Notes
                    </label>
                    <Textarea
                        id="description"
                        value={session.description}
                        onChange={(e) => handleChange('description', e.target.value)}
                        placeholder="Enter description notes"
                    />
                </div>

                <Button type="submit" className="w-full">
                    {sessionId ? 'Update' : 'Create'}
                </Button>
            </form>

            {user && user.fighterInfo?.role !== 0 && sessionId && (
                <div className="flex gap-2 mt-4">
                    <Button
                        type="button"
                        variant="secondary"
                        onClick={handleCloseSession}
                        disabled={isClosing}
                    >
                        {isClosing ? "Closing..." : "Close Session"}
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
                title={`Are you sure you want to ${sessionId ? 'update' : 'create'} a session?`}
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