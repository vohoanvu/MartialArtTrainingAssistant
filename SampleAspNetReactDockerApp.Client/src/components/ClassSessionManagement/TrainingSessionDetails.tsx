import { useEffect, useState } from 'react';
import {
    getTrainingSessionDetails,
    updateTrainingSessionDetails,
    generateClassCurriculum,
    getClassCurriculum,
    SuggestFighterPairs,
    removeStudentFromSession,
} from '@/services/api';
import { Button } from '@/components/ui/button';
import useAuthStore from '@/store/authStore';
import { MatchMakerRequest, SessionDetailViewModel, UpdateTrainingSessionRequest, CurriculumDto, ApiMatchMakerResponse, ApiMatchMakerResponseContent } from '@/types/global';
import { useParams } from 'react-router-dom';
import AttendancePage from './AttendancePage';
import ConfirmationDialog from '../ui/ConfirmationDialog';
import CurriculumSection from './CurriculumSection';
import { Trash2 } from 'lucide-react';

const TrainingSessionDetails = () => {
    const { sessionId } = useParams<{ sessionId: string }>();
    const sessionIdNumber = Number(sessionId);
    const [sessionDetails, setSessionDetails] = useState<SessionDetailViewModel | null>(null);
    const [loading, setLoading] = useState<boolean>(true);
    const [isLessonloading, setIsLessonLoading] = useState<boolean>(false);
    const [isPairingLoading, setIsPairingLoading] = useState<boolean>(false);
    const [error, setError] = useState<string | null>(null);
    const jwtToken = useAuthStore((state) => state.accessToken);
    const refreshToken = useAuthStore((state) => state.refreshToken);
    const user = useAuthStore((state) => state.user);
    const hydrate = useAuthStore((state) => state.hydrate);
    //const [fighterPairResult, setFighterPairResult] = useState<FighterPairResult>();
    const [matchMakeResponse, setMatchMakerResponse] = useState<ApiMatchMakerResponse>();
    const [suggestedPairingsContent, setSuggestedPairingsContent] = useState<ApiMatchMakerResponseContent | null>(null);
    const [curriculum, setCurriculum] = useState<CurriculumDto | null>(null);
    const [showAttendanceForm, setShowAttendanceForm] = useState(false);
    const [isAIDialogOpen, SetIsAIDialogOpen] = useState(false);
    const [isRemoving, setIsRemoving] = useState<number | null>(null);

    const pairingData = matchMakeResponse?.suggestedPairings ?? suggestedPairingsContent;

    useEffect(() => {
        const fetchSessionDetails = async () => {
            try {
                setLoading(true);
                const details = await getTrainingSessionDetails(sessionIdNumber, { jwtToken, refreshToken, hydrate });
                setSessionDetails(details);
                if (details && details.rawFighterPairsJson) {
                    const parsedFighterPairs = JSON.parse(details.rawFighterPairsJson) as ApiMatchMakerResponseContent;
                    setSuggestedPairingsContent(parsedFighterPairs);
                }
                if (details && details.isCurriculumGenerated) {
                    const savedCurriculum = await getClassCurriculum({sessionId: sessionIdNumber, jwtToken, refreshToken, hydrate});
                    setCurriculum(savedCurriculum);
                }
            } catch (err) {
                setError('Failed to load session details');
                console.error(err);
            } finally {
                setLoading(false);
            }
        };

        fetchSessionDetails();
    }, [hydrate, jwtToken, refreshToken, sessionIdNumber, showAttendanceForm]);

    const handleCheckIn = async () => {
        if (!user?.fighterInfo) {
            setError('Failed to retrieve fighter information');
            return;
        }

        try {
            const updateSessionRequest: UpdateTrainingSessionRequest = {
                studentIds: [user.fighterInfo.id]
            };
            const updatedSessionDetails = await updateTrainingSessionDetails(sessionIdNumber, updateSessionRequest, { jwtToken, refreshToken, hydrate });
            alert('You have successfully checked in!');
            setSessionDetails(updatedSessionDetails);
        } catch (err) {
            setError('Failed to check in');
            console.error(err);
        }
    };

    const handleRemoveStudent = async (studentId: number) => {
        try {
            setIsRemoving(studentId);
            await removeStudentFromSession(
                sessionIdNumber,
                studentId,
                { jwtToken, refreshToken, hydrate }
            );
            
            // Update the session details to refresh the roster
            const updatedDetails = await getTrainingSessionDetails(
                sessionIdNumber,
                { jwtToken, refreshToken, hydrate }
            );
            setSessionDetails(updatedDetails);
        } catch (err) {
            setError('Failed to remove student from session');
            console.error(err);
        } finally {
            setIsRemoving(null);
        }
    };

    const handlePairUp = async () => {
        if (!user?.fighterInfo) {
            setError('Failed to retrieve Instructor information');
            return;
        }
        setError(null);
        setIsPairingLoading(true);
        try {
            const generatePairRequest: MatchMakerRequest = {
                studentFighterIds: sessionDetails?.studentIds ?? [],
                instructorFighterId: user.fighterInfo.id
            };
            const matchMakeResponseData = await SuggestFighterPairs(generatePairRequest, sessionIdNumber, { jwtToken, hydrate });
            alert('Fighter Pair successfully generated');
            //setFighterPairResult(matchMakeResponse);
            setMatchMakerResponse(matchMakeResponseData);
        } catch (err) {
            setError('Failed to generate pairs');
            console.error(err);
        } finally {
            setIsPairingLoading(false);
        }
    };

    const handleGenerateCurriculum = async () => {
        setError(null);
        setIsLessonLoading(true);
        try {
            const curriculumData = await generateClassCurriculum({sessionId: sessionIdNumber, jwtToken, refreshToken, hydrate});
            setCurriculum(curriculumData);
        } catch (err) {
            setError('Failed to generate curriculum');
            console.error(err);
        } finally {
            setIsLessonLoading(false);
        }
    };

    const handleFeedback = (helpful: boolean) => {
        alert(`Feedback recorded: ${helpful ? 'Helpful' : 'Not Helpful'}`);
    };

    if (loading) return <p className="text-center text-lg text-muted-foreground">Loading...</p>;
    if (error) return <p className="text-center text-lg text-destructive">{error}</p>;

    return (
        <div className="container mx-auto max-w-6xl p-8 shadow-lg rounded-lg bg-card text-card-foreground transition-colors duration-300">
            <h1 className="text-3xl font-bold mb-6 text-center">Class Session Details</h1>
            <div className="flex flex-col md:flex-row gap-6">
                <div className="flex-1">
                    {sessionDetails?.instructor ? (
                        <div className="border border-border p-4 rounded-lg mb-6 bg-accent transition-colors">
                            <p><strong>Instructor Name:</strong> {sessionDetails.instructor.fighterName}</p>
                            <p><strong>Training Date:</strong> {sessionDetails.trainingDate}</p>
                            <p><strong>Capacity:</strong> {sessionDetails.capacity}</p>
                            <p><strong>Duration:</strong> {sessionDetails.duration} minutes</p>
                            <p><strong>Status:</strong> {sessionDetails.status}</p>
                            <p><strong>Level:</strong> {sessionDetails.targetLevel}</p>
                            <p><strong>Description Notes:</strong> {sessionDetails.description}</p>

                            {user && user.fighterInfo?.role === 0 ? (
                                <Button type="button" onClick={handleCheckIn} className="mt-4">
                                    Check-In
                                </Button>
                            ) : (
                                <div className="flex flex-wrap gap-2 mt-4">
                                    <Button
                                        type="button"
                                        onClick={() => setShowAttendanceForm(true)}
                                        variant='default'
                                    >
                                        Take Attendance
                                    </Button>
                                    <Button type="button" variant='outline'
                                        onClick={() => {
                                            window.confirm('Are you sure you want to pair up fighters? This will generate pairs based on the current roster.') && handlePairUp();
                                        }}
                                        disabled={isPairingLoading}
                                    >
                                        PAIR UP
                                    </Button>
                                    {isPairingLoading && (
                                        <div className="flex items-center space-x-2">
                                            <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-accent-foreground"></div> {/* Use a different color if needed, e.g., border-secondary */}
                                            <p className="text-accent-foreground">AI is matching fighters...</p>
                                        </div>
                                    )}
                                    <Button
                                        type="button"
                                        variant='default'
                                        onClick={() => SetIsAIDialogOpen(true)}
                                        disabled={isLessonloading}
                                    >
                                        Generate Today's Lessons
                                    </Button>
                                    {isLessonloading && (
                                        <div className="mt-4 flex items-center space-x-2">
                                            <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-primary"></div>
                                            <p className="text-primary">The AI is designing the curriculum for you. This may take a few minutes...</p>
                                        </div>
                                    )}
                                </div>
                            )}
                        </div>
                    ) : (
                        <p className="text-muted-foreground">No session details available.</p>
                    )}
                    <ConfirmationDialog
                        title="Generate AI Lesson Plan"
                        message="You are about to generate a new lesson plan using AI. This process may take a few minutes to complete. The AI will analyze your training requirements and create a structured curriculum for today's session."
                        isOpen={isAIDialogOpen}
                        onConfirm={async () => {
                            await handleGenerateCurriculum();
                            SetIsAIDialogOpen(false);
                        }}
                        onCancel={() => SetIsAIDialogOpen(false)}
                    />
                </div>

                <div className="flex-1 md:max-w-xs">
                    <div className="border border-border p-4 rounded-lg bg-accent transition-colors">
                        <h2 className="text-1xl font-bold">Students Roster</h2>
                        {sessionDetails && sessionDetails.students.length > 0 ? (
                            <ul className="list-disc pl-5 mt-2">
                                {sessionDetails.students.map((student) => (
                                    <li key={student.id} className="flex items-center justify-between">
                                        <span>{student.fighterName}</span>
                                        {user && user.fighterInfo?.role !== 0 && (
                                            <button
                                                onClick={() => handleRemoveStudent(student.id)}
                                                disabled={isRemoving === student.id}
                                                className="p-1 hover:bg-destructive/20 rounded-full transition-colors"
                                                title="Remove student from session"
                                            >
                                                {isRemoving === student.id ? (
                                                    <div className="animate-spin h-4 w-4 border-2 border-current border-t-transparent rounded-full" />
                                                ) : (
                                                    <Trash2 className="h-4 w-4 text-destructive hover:text-destructive/80" />
                                                )}
                                            </button>
                                        )}
                                    </li>
                                ))}
                            </ul>
                        ) : (
                            <p className="text-muted-foreground">No students enrolled in this session.</p>
                        )}
                    </div>
                </div>
            </div>

            {pairingData && pairingData.pairs.length > 0 && (
                <FighterPairs pairingData={pairingData} />
            )}

            {/* Attendance Taking layover */}
            {showAttendanceForm && (
                <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4">
                    <div className="bg-background p-6 rounded-lg w-[95vw] max-w-[1500px] max-h-[90vh] overflow-y">
                        <AttendancePage 
                            trainingSessionId={sessionIdNumber}
                            sessionDetailsViewModel={sessionDetails}
                            onCancel={() => setShowAttendanceForm(false)}
                        />
                    </div>
                </div>
            )}


            {/* Lesson Curriculum */}
            {curriculum && (
                <CurriculumSection 
                    curriculum={curriculum}
                    onFeedback={handleFeedback}
                />
            )}
        </div>
    );
};

export default TrainingSessionDetails;




interface FighterPairsProps {
    pairingData: ApiMatchMakerResponseContent;
}
const FighterPairs = ({ pairingData }: FighterPairsProps) => {
    return (
        <div className="mt-6">
            <h2 className="text-2xl font-bold">Fighter Pairs</h2>
            <ul className="mt-4 space-y-2">
                {pairingData.pairs.map((pair, index) => (
                    <li key={index} className="p-4 border border-border rounded-lg shadow-sm bg-background">
                        <p>
                            <strong>{pair.fighter1_name}</strong> VS <strong>{pair.fighter2_name}</strong>
                        </p>
                    </li>
                ))}
            </ul>
            {pairingData.unpaired_student ? (
                <div className="mt-2 p-4 bg-background border border-border rounded-md shadow-md">
                    <span className="text-xl font-bold mb-4 text-foreground">Unpaired Students</span>
                    <div className="bg-muted p-4 rounded-md overflow-auto">
                        <span className="font-semibold">
                            {pairingData.unpaired_student.studentId} : {pairingData.unpaired_student.studentName}
                        </span>
                        <pre className="text-sm text-foreground whitespace-pre-wrap">
                            {pairingData.unpaired_student.reason}
                        </pre>
                    </div>
                </div>
            ) : (
                <div className="mt-2 p-4 bg-background border border-border rounded-md shadow-md">
                    <span className="text-xl font-bold mb-4 text-foreground">Rationale</span>
                    <div className="bg-muted p-4 rounded-md overflow-auto">
                        <pre className="text-sm text-foreground whitespace-pre-wrap">
                            {pairingData.pairing_rationale}
                        </pre>
                    </div>
                </div>
            )}
        </div>
    );
};