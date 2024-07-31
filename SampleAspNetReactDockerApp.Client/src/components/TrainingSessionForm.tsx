import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import ConfirmationDialog from '@/components/ConfirmationDialog';  // Ensure this component is imported correctly
import { Button } from '@/components/ui/button';
import { CreateTrainingSessionRequest } from '@/types/global';
import { createTrainingSession } from '@/services/api';
import useAuthStore from '@/store/authStore';


const TrainingSessionForm = () => {
    const navigate = useNavigate();
    const jwtToken = useAuthStore((state) => state.accessToken);
    // const refreshToken = useAuthStore((state) => state.refreshToken);
    // const hydrate = useAuthStore((state) => state.hydrate);

    const [instructorName, setInstructorName] = useState('');
    const [trainingDate, setTrainingDate] = useState('');
    const [capacity, setCapacity] = useState('');
    const [duration, setDuration] = useState('');
    const [description, setDescription] = useState('');
    const [isDialogOpen, setIsDialogOpen] = useState(false);

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        setIsDialogOpen(true);

        console.log('Session Created:', {
            instructorName,
            trainingDate,
            capacity,
            duration,
            description,
        });
    };

    const handleConfirm = async () => {
        const newSession : CreateTrainingSessionRequest = {
            trainingDate,
            description,
            capacity: parseInt(capacity, 10),
            duration: parseInt(duration, 10),
            status: 'Active',
        };

        try {
            await createTrainingSession(newSession, jwtToken!);
            setIsDialogOpen(false);
            // After successful creation, navigate back to the dashboard or another page
            navigate('/dashboard');
        } catch (error) {
            console.error("Failed to create a new session:", error);
        }
    };

    const handleCancel = () => {
        setIsDialogOpen(false);
    };

    return (
        <div className="container mx-auto max-w-lg p-8 shadow-lg rounded-lg">
            <h1 className="text-3xl font-bold mb-6">Create New Session</h1>
            <form onSubmit={handleSubmit} className="space-y-4">
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
                    Create
                </Button>
            </form>

            <ConfirmationDialog
                title="Are you sure you want to create a new session?"
                message="You can update your session details later!"
                isOpen={isDialogOpen}
                onConfirm={handleConfirm}
                onCancel={handleCancel}
            />
        </div>
    );
};

export default TrainingSessionForm;