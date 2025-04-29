import { Button } from './ui/button';

interface FeedbackFormProps {
    videoId: number;
    timestamp: number;
    onSave: (data: {
        videoId: number,
        timestamp: number,
        feedbackText: string,
        feedbackType: string
    }) => void;
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
    videoId, 
    timestamp, 
    onSave,
    fromTimestamp,
    setFromTimestamp,
    toTimestamp,
    setToTimestamp,
    category,
    setCategory,
    feedbackText,
    setFeedbackText
}) => {
    
    return (
        <div className="p-4 border rounded-md shadow-md">
            <h3 className="text-lg font-semibold mb-4">Feedback</h3>
            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">From</label>
                <input
                    type="text"
                    value={fromTimestamp}
                    onChange={(e) => setFromTimestamp(e.target.value)}
                    className="w-full p-2 border rounded-md"
                />
            </div>
            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">To</label>
                <input
                    type="text"
                    value={toTimestamp}
                    onChange={(e) => setToTimestamp(e.target.value)}
                    className="w-full p-2 border rounded-md"
                />
            </div>
            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Feedback</label>
                <textarea
                    value={feedbackText}
                    onChange={(e) => setFeedbackText(e.target.value)}
                    className="w-full p-2 border rounded-md"
                />
            </div>
            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Category</label>
                <select
                    value={category}
                    onChange={(e) => setCategory(e.target.value)}
                    className="w-full p-2 border rounded-md"
                >
                    <option value="">Optional</option>
                    <option value="Posture">Posture</option>
                    <option value="Technique">Technique</option>
                    <option value="Defense">Defense</option>
                </select>
            </div>
            <div className="flex justify-end gap-2">
                <Button variant="secondary" onClick={() => onSave({ videoId, timestamp, feedbackText, feedbackType: category }) }>Save</Button>
                <Button variant="ghost" onClick={() => {
                    setFromTimestamp('');
                    setToTimestamp('');
                    setFeedbackText('');
                    setCategory('');
                }}>Cancel</Button>
            </div>
        </div>
    );
};

export default FeedbackForm;