import { useEffect, useState } from 'react';
import { 
    getTrainingSessionDetails, 
    updateTrainingSessionDetails, 
    getFighterInfo,
    GenerateFighterPairs
} from '@/services/api';// Assuming these API functions exist
import { Button } from '@/components/ui/button';
import useAuthStore from '@/store/authStore';
import { FighterInfo, FighterPairResult, MatchMakerRequest, SessionDetailViewModel, UpdateTrainingSessionRequest } from '@/types/global';
import { useParams } from 'react-router-dom';

const TrainingSessionDetails = () => {
    const { sessionId  } = useParams<{ sessionId: string }>();
    const sessionIdNumber = Number(sessionId);
    const [sessionDetails, setSessionDetails] = useState<SessionDetailViewModel | null>(null);
    const [loading, setLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);
    const jwtToken = useAuthStore((state) => state.accessToken);
    const refreshToken = useAuthStore((state) => state.refreshToken);
    const hydrate = useAuthStore((state) => state.hydrate);
    const [fighterInfo, setFighterInfo] = useState<FighterInfo>();
    const [fighterPairResult, setFighterPairResult] = useState<FighterPairResult>();

    useEffect(() => {
        const fetchSessionDetails = async () => {
            try {
                setLoading(true);
                const details = await getTrainingSessionDetails(sessionIdNumber, { jwtToken, refreshToken, hydrate });
                setSessionDetails(details);
                const authenticatedFighter = await getFighterInfo({ jwtToken, refreshToken, hydrate });
                setFighterInfo(authenticatedFighter);
            } catch (err) {
                setError('Failed to load session details');
                console.error(err);
            } finally {
                setLoading(false);
            }
        };

        fetchSessionDetails();
    }, [hydrate, jwtToken, refreshToken, sessionIdNumber]);

    const handleCheckIn = async () => {
        if (!fighterInfo) {
            setError('Failed to retrieve fighter information');
            return;
        }

        try {
            const updateSessionRequest : UpdateTrainingSessionRequest = {
                studentIds: [ fighterInfo.fighter.id ]
            }
            const updatedSessionDetails =  await updateTrainingSessionDetails(sessionIdNumber, updateSessionRequest, { jwtToken, refreshToken, hydrate });
            alert('You have successfully checked in!');
            setSessionDetails(updatedSessionDetails);
        } catch (err) {
            setError('Failed to check in');
            console.error(err);
        }
    };

    const handlePairUp = async () => {
        if (!fighterInfo) {
            setError('Failed to retrieve fighter information');
            return;
        }

        try {
            const generatePairRequest : MatchMakerRequest = {
                studentFighterIds: sessionDetails?.studentIds ?? [],
                instructorFighterId: fighterInfo.fighter.id
            }
            const fighterPairResult = await GenerateFighterPairs(
                generatePairRequest, 
                { jwtToken, hydrate: () => {} }
            );
            alert('Fighter Pair succesfully generated');
            setFighterPairResult(fighterPairResult);
        } catch (err) {
            setError('Failed generate pairs');
            console.error(err);
        }
    };

    if (loading) return <p>Loading...</p>;
    if (error) return <p>{error}</p>;

    return (
        <div className="container mx-auto max-w-lg p-8 shadow-lg rounded-lg">
            <h1 className="text-3xl font-bold mb-6 text-center">Session Details</h1>
            {
                sessionDetails?.instructor ? (
                    <div className="border p-4 rounded-lg">
                        <p><strong>Instructor Name:</strong> {sessionDetails.instructor.fighterName}</p>
                        <p><strong>Training Date:</strong> {sessionDetails.trainingDate}</p>
                        <p><strong>Capacity:</strong> {sessionDetails.capacity}</p>
                        <p><strong>Duration:</strong> {sessionDetails.duration} hour(s)</p>
                        <p><strong>Status:</strong> {sessionDetails.status}</p>
                        <p><strong>Description Notes:</strong> {sessionDetails.description}</p>
                        <div>
                            <h2 className="text-2xl font-bold mt-4">Students roster</h2>
                            {
                                sessionDetails && sessionDetails.students.length > 0 ? (
                                    <>
                                        {console.log("Rendering students list:", sessionDetails.students)}
                                        <ul className="list-disc pl-5">
                                            {sessionDetails.students.map((student) => (
                                                <li key={student.id}>{student.fighterName}</li>
                                            ))}
                                        </ul>
                                    </>
                                ) : (
                                    <p>No students enrolled in this session.</p>
                                )
                            }
                        </div>

                        {
                            fighterPairResult && fighterPairResult.length > 0 && (
                                <div className="mt-6">
                                    <h2 className="text-2xl font-bold">Fighter Pairs</h2>
                                    <ul className="mt-4 space-y-2">
                                        {
                                            fighterPairResult.map((pair, index) => (
                                                <li key={index} className="p-4 border rounded-lg shadow-sm bg-gray-50">
                                                    {/* <p><strong>Fighter 1:</strong> {pair.fighter1}</p>
                                                    <p><strong>Fighter 2:</strong> {pair.fighter2}</p> */}
                                                    <p><strong>{pair.fighter1}</strong> VS <strong>{pair.fighter2}</strong></p>
                                                </li>
                                            ))
                                        }
                                    </ul>
                                </div>
                            )
                        }

                        {
                            fighterInfo && fighterInfo.fighter.role == 0 ? (
                                <Button type="button" onClick={handleCheckIn} className="mt-4">
                                    Check-In
                                </Button>
                            ) : (
                                <Button type="button" onClick={handlePairUp} className="mt-4">
                                    PAIR UP
                                </Button>
                            )
                        }
                    </div>
                ) : (
                    <p>No session details available.</p>
                )
            }
        </div>
    );
};

export default TrainingSessionDetails;
