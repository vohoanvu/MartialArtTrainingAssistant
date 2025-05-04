import { useEffect, useState } from 'react';
import { Button } from './ui/button';
import { Fighter, MartialArt } from '@/types/global';
import { use } from 'i18next';

interface FeedbackFormProps {
    videoId: number;
    onSave: () => void;
    onCancel: () => void;
    fromTimestamp: string;
    setFromTimestamp: (timestamp: string) => void;
    toTimestamp: string;
    setToTimestamp: (timestamp: string) => void;
    feedbackText: string;
    setFeedbackText: (text: string) => void;
    fighterDetails: Fighter;
}

const FeedbackForm: React.FC<FeedbackFormProps> = ({
    onSave,
    onCancel,
    fromTimestamp,
    setFromTimestamp,
    toTimestamp,
    setToTimestamp,
    feedbackText,
    setFeedbackText,
    fighterDetails,
}) => {
    const [martialArt, setMartialArt] = useState<MartialArt>(MartialArt.None);
    const [studentName, setStudentName] = useState('');
    const [beltRantk, setBeltRank] = useState('');
    const [studentIdentifier, setStudentIdentifier] = useState('');
    const [positionalScenario, setPositionalScenario] = useState('');
    const [techniqueType, setTechniqueType] = useState('');
    const [techniqueName, setTechniqueName] = useState('');

    useEffect(() => {
        if (fighterDetails) {
            setStudentName(fighterDetails.fighterName);
            setBeltRank(fighterDetails.beltColor);
            setStudentIdentifier("TO BE CONTINUED");
        }
    }, [fighterDetails]);

    const formatTimestamp = (seconds: number): string => {
        const minutes = Math.floor(seconds / 60);
        const remainingSeconds = Math.floor(seconds % 60);
        return `${minutes.toString().padStart(2, '0')}:${remainingSeconds.toString().padStart(2, '0')}`;
    };

    const isBJJMartialArt = (art: MartialArt) =>
        art === MartialArt.BrazilianJiuJitsu_GI || art === MartialArt.BrazilianJiuJitsu_NO_GI;

    return (
        <div className="p-4 border rounded-md shadow-md">
            <h3 className="text-lg font-semibold mb-4">Feedback</h3>
            <div className="flex gap-4 mb-4">
                <div className="flex-1">
                    <label className="block text-sm font-medium mb-1">From</label>
                    <input
                        type="text"
                        value={fromTimestamp ? formatTimestamp(parseFloat(fromTimestamp)) : ''}
                        onChange={(e) => {
                            const [minutes, seconds] = e.target.value.split(':').map(Number);
                            if (!isNaN(minutes) && !isNaN(seconds)) {
                                setFromTimestamp((minutes * 60 + seconds).toString());
                            }
                        }}
                        className="w-full p-2 border rounded-md"
                        placeholder="MM:SS"
                    />
                </div>
                <div className="flex-1">
                    <label className="block text-sm font-medium mb-1">To</label>
                    <input
                        type="text"
                        value={toTimestamp ? formatTimestamp(parseFloat(toTimestamp)) : ''}
                        onChange={(e) => {
                            const [minutes, seconds] = e.target.value.split(':').map(Number);
                            if (!isNaN(minutes) && !isNaN(seconds)) {
                                setToTimestamp((minutes * 60 + seconds).toString());
                            }
                        }}
                        className="w-full p-2 border rounded-md"
                        placeholder="MM:SS"
                    />
                </div>
            </div>

            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Martial Art</label>
                <select
                    value={martialArt}
                    onChange={(e) => setMartialArt(e.target.value as MartialArt)}
                    className="w-full p-2 border rounded-md"
                >
                    <option value={MartialArt.None}>Select a martial art</option>
                    {Object.values(MartialArt).map((art) => (
                        <option key={art} value={art}>
                            {art}
                        </option>
                    ))}
                </select>
            </div>

            {isBJJMartialArt(martialArt) && (
                <div id='bjjFields'>
                    <div className="mb-4">
                        <label className="block text-sm font-medium mb-1">Student Name</label>
                        <input
                            type="text"
                            value={studentName}
                            onChange={(e) => setStudentName(e.target.value)}
                            className="w-full p-2 border rounded-md"
                            placeholder="Enter student name..."
                            readOnly={true}
                        />
                    </div>

                    <div className="mb-4">
                        <label className="block text-sm font-medium mb-1">Belt Rank</label>
                        <input
                            type="text"
                            value={beltRantk}
                            onChange={(e) => setBeltRank(e.target.value)}
                            className="w-full p-2 border rounded-md"
                            placeholder="Enter belt rank..."
                            readOnly={true}
                        />
                    </div>

                    <div className="mb-4">
                        <label className="block text-sm font-medium mb-1">Student Identifier</label>
                        <input
                            type="text"
                            value={studentIdentifier}
                            onChange={(e) => setStudentIdentifier(e.target.value)}
                            className="w-full p-2 border rounded-md"
                            placeholder="Enter student identifier..."
                            readOnly={true}
                        />
                    </div>

                    <div className="mb-4">
                        <label className="block text-sm font-medium mb-1">Positional Scenario</label>
                        <input
                            type="text"
                            value={positionalScenario}
                            onChange={(e) => setPositionalScenario(e.target.value)}
                            className="w-full p-2 border rounded-md"
                            placeholder="Enter positional scenario..."
                        />
                    </div>

                    <div className="mb-4">
                        <label className="block text-sm font-medium mb-1">Technique Type</label>
                        <input
                            type="text"
                            value={techniqueType}
                            onChange={(e) => setTechniqueType(e.target.value)}
                            className="w-full p-2 border rounded-md"
                            placeholder="Enter technique type..."
                        />
                    </div>

                    <div className="mb-4">
                        <label className="block text-sm font-medium mb-1">Technique Name</label>
                        <input
                            type="text"
                            value={techniqueName}
                            onChange={(e) => setTechniqueName(e.target.value)}
                            className="w-full p-2 border rounded-md"
                            placeholder="Enter technique name..."
                        />
                    </div>

                    <div className="mb-4">
                        <label className="block text-sm font-medium mb-1">Overall Feedback</label>
                        <textarea
                            value={feedbackText}
                            onChange={(e) => setFeedbackText(e.target.value)}
                            className="w-full h-24 p-2 border rounded-md mb-2"
                            placeholder="Enter feedback..."
                        />
                    </div>
                </div>
            )}

            <div className="flex justify-end gap-2">
                <Button variant="default" onClick={onSave}>Save</Button>
                <Button variant="secondary" onClick={onCancel}>Cancel</Button>
            </div>
        </div>
    );
};

export default FeedbackForm;