import { Button } from './ui/button';

interface FeedbackFormProps {
    videoId: number;
    timestamp: number;
    onSave: () => void;
    onCancel: () => void;
    fromTimestamp: string;
    setFromTimestamp: (timestamp: string) => void;
    toTimestamp: string;
    setToTimestamp: (timestamp: string) => void;
    category: string;
    setCategory: (category: string) => void;
    feedbackText: string;
    setFeedbackText: (text: string) => void;
}

const FeedbackForm: React.FC<FeedbackFormProps> = ({
    onSave,
    onCancel,
    fromTimestamp,
    setFromTimestamp,
    toTimestamp,
    setToTimestamp,
    category,
    setCategory,
    feedbackText,
    setFeedbackText
}) => {

    const formatTimestamp = (seconds: number): string => {
        const minutes = Math.floor(seconds / 60);
        const remainingSeconds = Math.floor(seconds % 60);
        return `${minutes.toString().padStart(2, '0')}:${remainingSeconds.toString().padStart(2, '0')}`;
    };

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
                <label className="block text-sm font-medium mb-1">Feedback</label>
                <textarea
                    value={feedbackText}
                    onChange={(e) => setFeedbackText(e.target.value)}
                    className="w-full h-24 p-2 border rounded-md mb-2"
                    placeholder="Enter feedback..."
                />
            </div>
            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Category</label>
                <select
                    value={category}
                    onChange={(e) => setCategory(e.target.value)}
                    className="w-full p-2 border rounded-md"
                >
                    <option value="">No options</option>
                    <option value="Posture">Posture</option>
                    <option value="Technique Execution">Technique Execution</option>
                    <option value="Defense">Defense</option>
                    <option value="Movement Efficiency">Movement Efficiency</option>
                </select>
            </div>
            <div className="flex justify-end gap-2">
                <Button variant="secondary" onClick={onSave}>Save</Button>
                <Button variant="ghost" onClick={onCancel}>Cancel</Button>
            </div>
        </div>
    );
};

export default FeedbackForm;