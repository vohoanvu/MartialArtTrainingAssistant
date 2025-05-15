import { useEffect, useState } from 'react';
import {
    getTrainingSessionDetails,
    updateTrainingSessionDetails,
    GenerateFighterPairs,
    generateClassCurriculum,
    getClassCurriculum,
} from '@/services/api';
import { Button } from '@/components/ui/button';
import useAuthStore from '@/store/authStore';
import { FighterPairResult, GetBMIResponse, MatchMakerRequest, SessionDetailViewModel, UpdateTrainingSessionRequest, CurriculumDto } from '@/types/global';
import { useParams } from 'react-router-dom';

const TrainingSessionDetails = () => {
    const { sessionId } = useParams<{ sessionId: string }>();
    const sessionIdNumber = Number(sessionId);
    const [sessionDetails, setSessionDetails] = useState<SessionDetailViewModel | null>(null);
    const [loading, setLoading] = useState<boolean>(true);
    const [isAIloading, setIsAIloading] = useState<boolean>(false);
    const [error, setError] = useState<string | null>(null);
    const jwtToken = useAuthStore((state) => state.accessToken);
    const refreshToken = useAuthStore((state) => state.refreshToken);
    const user = useAuthStore((state) => state.user);
    const hydrate = useAuthStore((state) => state.hydrate);
    const [fighterPairResult, setFighterPairResult] = useState<FighterPairResult>();
    const [instructorBMI, setInstructorBMI] = useState<GetBMIResponse>();
    const [expandedSections, setExpandedSections] = useState<{ [key: string]: boolean }>({});
    const [curriculum, setCurriculum] = useState<CurriculumDto | null>(null);
    const [notes, setNotes] = useState<string>('');

    useEffect(() => {
        const fetchSessionDetails = async () => {
            try {
                setLoading(true);
                const details = await getTrainingSessionDetails(sessionIdNumber, { jwtToken, refreshToken, hydrate });
                setSessionDetails(details);
                const savedCurriculum = await getClassCurriculum({sessionId: sessionIdNumber, jwtToken, refreshToken, hydrate});
                setCurriculum(savedCurriculum);
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

    const handlePairUp = async () => {
        if (!user?.fighterInfo) {
            setError('Failed to retrieve fighter information');
            return;
        }

        try {
            const generatePairRequest: MatchMakerRequest = {
                studentFighterIds: sessionDetails?.studentIds ?? [],
                instructorFighterId: user.fighterInfo.id
            };
            const fighterPairResult = await GenerateFighterPairs(generatePairRequest, { jwtToken, hydrate: () => { } });
            alert('Fighter Pair successfully generated');
            setFighterPairResult(fighterPairResult);
        } catch (err) {
            setError('Failed to generate pairs');
            console.error(err);
        }
    };

    // const handleCalculateBMI = async () => {
    //     if (!user?.fighterInfo) {
    //         setError('Failed to retrieve fighter information');
    //         return;
    //     }

    //     try {
    //         const result = await CalculateBMI(user.fighterInfo.height, user.fighterInfo.weight);
    //         setInstructorBMI(result);
    //     } catch (err) {
    //         setError('Failed to calculate BMI');
    //         console.error(err);
    //     }
    // };

    const toggleAccordion = (id: string) => {
        setExpandedSections((prev) => ({
            ...prev,
            [id]: !prev[id]
        }));
    };

    const handleGenerateCurriculum = async () => {
        setError(null);
        setIsAIloading(true);
        try {
            const curriculumData = await generateClassCurriculum({sessionId: sessionIdNumber, jwtToken, refreshToken, hydrate});
            setCurriculum(curriculumData);
        } catch (err) {
            setError('Failed to generate curriculum');
            console.error(err);
        } finally {
            setIsAIloading(false);
        }
    };

    const startTimer = (minutes: number) => {
        const confirm = window.confirm(`Start a ${minutes}-minute timer?`);
        if (confirm) {
            let seconds = minutes * 60;
            const interval = setInterval(() => {
                if (seconds <= 0) {
                    clearInterval(interval);
                    alert('Timer finished!');
                }
                seconds--;
            }, 1000);
        }
    };

    const handleFeedback = (helpful: boolean) => {
        alert(`Feedback recorded: ${helpful ? 'Helpful' : 'Not Helpful'}`);
    };

    function formatDescriptionWithNumberedList(text: string) {
        // Regex to match "1. ... 2. ... 3. ..." style lists
        const numberedListRegex = /(\d+\.\s[^.]+(?:\.[^0-9]|$))/g;
        const matches = text.match(numberedListRegex);

        if (matches && matches.length > 1) {
            // Split the text before the first number
            const firstNumberIndex = text.search(/\d+\.\s/);
            const beforeList = text.slice(0, firstNumberIndex).trim();
            const listItems = text
                .slice(firstNumberIndex)
                .split(/\d+\.\s/)
                .filter(Boolean)
                .map(item => item.replace(/^\s*|\s*\.$/g, '').trim());

            return (
                <div>
                    {beforeList && <p>{beforeList}</p>}
                    <ol className="list-decimal ml-6">
                        {listItems.map((item, idx) => (
                            <li key={idx}>{item}</li>
                        ))}
                    </ol>
                </div>
            );
        }
        // Fallback: just return as plain text
        return <p>{text}</p>;
    }

    if (loading) return <p className="text-center text-lg text-muted-foreground">Loading...</p>;
    if (error) return <p className="text-center text-lg text-destructive">{error}</p>;

    return (
        <div className="container mx-auto max-w-4xl p-8 shadow-lg rounded-lg bg-card text-card-foreground transition-colors duration-300">
            <h1 className="text-3xl font-bold mb-6 text-center">Session Details</h1>
            {sessionDetails?.instructor ? (
                <div className="border border-border p-4 rounded-lg mb-6 bg-accent transition-colors">
                    <p><strong>Instructor Name:</strong> {sessionDetails.instructor.fighterName}</p>
                    <p><strong>Training Date:</strong> {sessionDetails.trainingDate}</p>
                    <p><strong>Capacity:</strong> {sessionDetails.capacity}</p>
                    <p><strong>Duration:</strong> {sessionDetails.duration} minutes</p>
                    <p><strong>Status:</strong> {sessionDetails.status}</p>
                    <p><strong>Description Notes:</strong> {sessionDetails.description}</p>
                    {instructorBMI && (
                        <div className="mt-2 space-y-1">
                            <p><strong>Instructor BMI:</strong> {instructorBMI.bmi}</p>
                            <p><strong>Category:</strong> {instructorBMI.category}</p>
                            <p><strong>Description:</strong> {instructorBMI.description}</p>
                        </div>
                    )}

                    <div className="mt-4">
                        <h2 className="text-2xl font-bold">Students Roster</h2>
                        {sessionDetails && sessionDetails.students.length > 0 ? (
                            <ul className="list-disc pl-5 mt-2">
                                {sessionDetails.students.map((student) => (
                                    <li key={student.id}>{student.fighterName}</li>
                                ))}
                            </ul>
                        ) : (
                            <p className="text-muted-foreground">No students enrolled in this session.</p>
                        )}
                    </div>

                    {fighterPairResult && fighterPairResult.length > 0 && (
                        <div className="mt-6">
                            <h2 className="text-2xl font-bold">Fighter Pairs</h2>
                            <ul className="mt-4 space-y-2">
                                {fighterPairResult.map((pair, index) => (
                                    <li key={index} className="p-4 border border-border rounded-lg shadow-sm bg-background">
                                        <p>
                                            <strong>{pair.fighter1}</strong> VS <strong>{pair.fighter2}</strong>
                                        </p>
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}

                    {user && user.fighterInfo?.role === 0 ? (
                        <Button type="button" onClick={handleCheckIn} className="mt-4">
                            Check-In
                        </Button>
                    ) : (
                        <div className="flex flex-wrap gap-2 mt-4">
                            <Button type="button" variant='outline' onClick={handlePairUp}>
                                PAIR UP
                            </Button>
                            <Button
                                type="button"
                                variant='default'
                                onClick={async () => {
                                    if (window.confirm("Are you sure you want to generate today's lessons? This may take a few minutes.")) {
                                        await handleGenerateCurriculum();
                                    }
                                }}
                                disabled={isAIloading}
                            >
                                Generate Today's Lessons
                            </Button>
                            {isAIloading && (
                                <div className="mt-4 flex items-center space-x-2">
                                    <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-primary"></div>
                                    <p className="text-primary">The AI is design the curriculum for you. This may take a few minutes.</p>
                                </div>
                            )}
                        </div>
                    )}
                </div>
            ) : (
                <p className="text-muted-foreground">No session details available.</p>
            )}


            {/* Curriculum Section */}
            {curriculum && (
                <div className="mt-8">
                    <h2 className="text-3xl font-bold mb-4 text-center">{curriculum.session_title}</h2>
                    <p className="text-lg text-muted-foreground mb-6 text-center">Total Duration: {curriculum.duration}</p>

                    {/* Navigation Bar */}
                    <nav className="flex flex-wrap justify-center space-x-4 mb-6 bg-accent text-accent-foreground p-2 rounded-lg sticky top-0 z-10 shadow">
                        <a href="#warm-up" className="hover:underline focus:underline transition-colors">Warm-Up</a>
                        <a href="#techniques" className="hover:underline focus:underline transition-colors">Techniques</a>
                        <a href="#drills" className="hover:underline focus:underline transition-colors">Drills</a>
                        <a href="#sparring" className="hover:underline focus:underline transition-colors">Sparring</a>
                        <a href="#cool-down" className="hover:underline focus:underline transition-colors">Cool-Down</a>
                    </nav>

                    {/* Warm-Up */}
                    <section id="warm-up" className="mb-6">
                        <div className="bg-orange-100 dark:bg-orange-900 p-4 rounded-lg shadow">
                            <div className="flex items-center justify-between">
                                <h3 className="text-2xl font-semibold text-orange-800 dark:text-orange-200">Warm-Up</h3>
                                <Button onClick={() => startTimer(10)} className="bg-orange-500 hover:bg-orange-600 text-white">
                                    Start Timer
                                </Button>
                            </div>
                            <p className="text-lg font-bold mt-2">{curriculum.warm_up.name}</p>
                            <p className="text-muted-foreground">{curriculum.warm_up.description}</p>
                            <p className="text-sm text-muted-foreground">Duration: {curriculum.warm_up.duration}</p>
                        </div>
                    </section>

                    {/* Techniques */}
                    <section id="techniques" className="mb-6">
                        <div className="bg-blue-100 dark:bg-blue-900 p-4 rounded-lg shadow">
                            <h3 className="text-2xl font-semibold text-blue-800 dark:text-blue-200">Techniques</h3>
                            <div className="mt-2">
                                {curriculum.techniques.map((tech, index) => (
                                    <div key={index} className="border-b border-blue-200 dark:border-blue-800 py-2">
                                        <button
                                            className="w-full text-left text-lg font-bold text-blue-700 dark:text-blue-300 flex justify-between items-center focus:outline-none"
                                            onClick={() => toggleAccordion(`tech-${index}`)}
                                        >
                                            {tech.name}
                                            <span>{expandedSections[`tech-${index}`] ? '▲' : '▼'}</span>
                                        </button>
                                        <div className={`mt-2 ${expandedSections[`tech-${index}`] ? 'block' : 'hidden'}`}>
                                            {formatDescriptionWithNumberedList(tech.description)}
                                            <p className="relative group">
                                                <span className="font-semibold">Tips: </span>
                                                <span className="underline decoration-dotted cursor-help group-hover:no-underline">{tech.tips}</span>
                                                <span className="absolute hidden group-hover:block bg-background text-foreground text-sm rounded p-2 -mt-10 w-64 shadow-lg z-20">
                                                    {tech.tips}
                                                </span>
                                            </p>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </section>

                    {/* Drills */}
                    <section id="drills" className="mb-6">
                        <div className="bg-green-100 dark:bg-green-900 p-4 rounded-lg shadow">
                            <h3 className="text-2xl font-semibold text-green-800 dark:text-green-200">Drills</h3>
                            <div className="mt-2">
                                {curriculum.drills.map((drill, index) => (
                                    <div key={index} className="border-b border-green-200 dark:border-green-800 py-2">
                                        <button
                                            className="w-full text-left text-lg font-bold text-green-700 dark:text-green-300 flex justify-between items-center focus:outline-none"
                                            onClick={() => toggleAccordion(`drill-${index}`)}
                                        >
                                            {drill.name}
                                            <span>{expandedSections[`drill-${index}`] ? '▲' : '▼'}</span>
                                        </button>
                                        <div className={`mt-2 ${expandedSections[`drill-${index}`] ? 'block' : 'hidden'}`}>
                                            <p>{drill.description}</p>
                                            <p><strong>Focus:</strong> {drill.focus}</p>
                                            <p className="text-sm text-muted-foreground">Duration: {drill.duration}</p>
                                            <Button
                                                onClick={() => startTimer(parseInt(drill.duration.split(' ')[0]))}
                                                className="mt-2 bg-green-500 hover:bg-green-600 text-white"
                                            >
                                                Start Timer
                                            </Button>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </section>

                    {/* Sparring */}
                    <section id="sparring" className="mb-6">
                        <div className="bg-red-100 dark:bg-red-900 p-4 rounded-lg shadow">
                            <div className="flex items-center justify-between">
                                <h3 className="text-2xl font-semibold text-red-800 dark:text-red-200">Sparring</h3>
                                <Button onClick={() => startTimer(15)} className="bg-red-500 hover:bg-red-600 text-white">
                                    Start Timer
                                </Button>
                            </div>
                            <p className="text-lg font-bold mt-2">{curriculum.sparring.name}</p>
                            {formatDescriptionWithNumberedList(curriculum.sparring.description)}
                            <p className="font-semibold">Guidelines:</p>
                            <ul className="list-disc ml-6">
                                {(curriculum.sparring.guidelines || '').split(';').map((guideline, index) => (
                                    <li key={index}>{guideline.trim()}</li>
                                ))}
                            </ul>
                            <p className="text-sm text-muted-foreground">Duration: {curriculum.sparring.duration}</p>
                        </div>
                    </section>

                    {/* Cool-Down */}
                    <section id="cool-down" className="mb-6">
                        <div className="bg-purple-100 dark:bg-purple-900 p-4 rounded-lg shadow">
                            <div className="flex items-center justify-between">
                                <h3 className="text-2xl font-semibold text-purple-800 dark:text-purple-200">Cool-Down</h3>
                                <Button onClick={() => startTimer(10)} className="bg-purple-500 hover:bg-purple-600 text-white">
                                    Start Timer
                                </Button>
                            </div>
                            <p className="text-lg font-bold mt-2">{curriculum.cool_down.name}</p>
                            <p className="text-muted-foreground">{curriculum.cool_down.description}</p>
                            <p className="text-sm text-muted-foreground">Duration: {curriculum.cool_down.duration}</p>
                        </div>
                    </section>

                    {/* Notes */}
                    <section id="notes" className="mb-6">
                        <div className="bg-accent p-4 rounded-lg shadow">
                            <h3 className="text-2xl font-semibold">Instructor Notes</h3>
                            <textarea
                                className="w-full h-24 p-2 mt-2 border rounded bg-background border-border focus:ring-2 focus:ring-primary"
                                placeholder="Record observations or adjustments during the session..."
                                value={notes}
                                onChange={(e) => setNotes(e.target.value)}
                            ></textarea>
                        </div>
                    </section>

                    {/* Feedback */}
                    <section id="feedback" className="mb-6">
                        <div className="bg-accent p-4 rounded-lg shadow">
                            <h3 className="text-2xl font-semibold">Feedback</h3>
                            <p className="text-lg mt-2">Was this helpful?</p>
                            <div className="flex space-x-4 mt-2">
                                <Button
                                    onClick={() => handleFeedback(true)}
                                    className="bg-green-600 hover:bg-green-700 text-white"
                                >
                                    Yes
                                </Button>
                                <Button
                                    onClick={() => handleFeedback(false)}
                                    className="bg-red-600 hover:bg-red-700 text-white"
                                >
                                    No
                                </Button>
                            </div>
                            <textarea
                                className="w-full h-24 p-2 mt-4 border rounded bg-background border-border focus:ring-2 focus:ring-primary"
                                placeholder="Share your feedback or suggestions..."
                            ></textarea>
                            <Button className="mt-2 bg-primary hover:bg-primary/90 text-primary-foreground">Submit Feedback</Button>
                        </div>
                    </section>
                </div>
            )}
        </div>
    );
};

export default TrainingSessionDetails;