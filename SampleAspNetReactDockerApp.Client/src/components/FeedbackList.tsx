import { Feedback } from "./AiInsights";

interface FeedbackListProps {
    feedback: Feedback[];
    onSelect: (timestamp: number) => void;
}

const FeedbackList: React.FC<FeedbackListProps> = ({ feedback, onSelect }) => (
    <div className="w-64 bg-gray-100 p-4">
        {feedback.map(f => (
            <div key={f.id} onClick={() => onSelect(f.timestamp)} className="cursor-pointer">
                <p>{f.timestamp.toFixed(2)}s - {f.feedbackType}</p>
                <p>{f.feedbackText.substring(0, 50)}...</p>
            </div>
        ))}
    </div>
);

export default FeedbackList;