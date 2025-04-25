import { useState } from 'react';

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
}

const FeedbackForm: React.FC<FeedbackFormProps> = ({ videoId, timestamp, onSave, onCancel }) => {
    const [feedbackText, setFeedbackText] = useState('');
    const [feedbackType, setFeedbackType] = useState('Posture');

    const handleSave = () => {
        onSave({ videoId, timestamp, feedbackText, feedbackType });
        setFeedbackText('');
    };

    return (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 flex items-center justify-center">
            <div className="bg-white p-4 rounded">
                <h2>Give Feedback</h2>
                <p>Timestamp: {timestamp.toFixed(2)}s</p>
                <select value={feedbackType} onChange={e => setFeedbackType(e.target.value)}>
                    <option value="Posture">Posture</option>
                    <option value="Technique Execution">Technique Execution</option>
                </select>
                <textarea value={feedbackText} onChange={e => setFeedbackText(e.target.value)} />
                <button onClick={handleSave}>Save</button>
                <button onClick={onCancel}>Cancel</button>
            </div>
        </div>
    );
};

export default FeedbackForm;