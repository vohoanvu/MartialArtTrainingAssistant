import React, { useEffect, useState } from 'react';
import { getTrainingSessionDetails, updateTrainingSessionDetails } from '@/services/api';// Assuming these API functions exist
import { Button } from '@/components/ui/button';
import useAuthStore from '@/store/authStore';
import { SessionDetailViewModel, UpdateTrainingSessionRequest } from '@/types/global';
import { useNavigate } from 'react-router-dom';


export interface ActionCellProps {
    sessionId: number;
}

const TrainingSessionDetails: React.FC<ActionCellProps> = ({ sessionId }) => {
    const [sessionDetails, setSessionDetails] = useState<SessionDetailViewModel | null>(null);
    const [loading, setLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);
    const jwtToken = useAuthStore((state) => state.accessToken);
    const refreshToken = useAuthStore((state) => state.refreshToken);
    const hydrate = useAuthStore((state) => state.hydrate);
    const navigate = useNavigate();

    useEffect(() => {
        const fetchSessionDetails = async () => {
            try {
                const details = await getTrainingSessionDetails(sessionId, { jwtToken, refreshToken, hydrate });
                setSessionDetails(details);
            } catch (err) {
                setError('Failed to load session details');
                console.error(err);
            } finally {
                setLoading(false);
            }
        };

        fetchSessionDetails();
    }, [hydrate, jwtToken, refreshToken, sessionId]);

    const handleCheckIn = async () => {
        try {
            const updateSessionRequest : UpdateTrainingSessionRequest = {
                instructorId: 1,
                studentIds: [1, 2, 3 ,4]
            }
            await updateTrainingSessionDetails(sessionId, updateSessionRequest, { jwtToken, refreshToken, hydrate });
            alert('You have successfully checked in!');
            navigate('/dashboard'); // Navigate back to the dashboard or wherever appropriate
        } catch (err) {
            setError('Failed to check in');
            console.error(err);
        }
    };

    if (loading) return <p>Loading...</p>;
    if (error) return <p>{error}</p>;

    return (
        <div className="container mx-auto max-w-lg p-8 shadow-lg rounded-lg">
            <h1 className="text-3xl font-bold mb-6 text-center">Session Details</h1>
            {sessionDetails && (
                <div className="border p-4 rounded-lg">
                    <p><strong>Instructor Name:</strong> {sessionDetails.instructor.fighterName}</p>
                    <p><strong>Training Date:</strong> {sessionDetails.trainingDate}</p>
                    <p><strong>Capacity:</strong> {sessionDetails.capacity}</p>
                    <p><strong>Duration:</strong> {sessionDetails.duration} hour(s)</p>
                    <p><strong>Status:</strong> {sessionDetails.status}</p>
                    <p><strong>Description Notes:</strong> {sessionDetails.description}</p>
                    <Button type="button" onClick={handleCheckIn} className="mt-4">
                        Check-In
                    </Button>
                </div>
            )}
        </div>
    );
};

export default TrainingSessionDetails;
