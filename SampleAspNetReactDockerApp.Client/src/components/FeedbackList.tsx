import React from 'react';

interface Feedback {
    id: string;
    timestamp: number;
    feedback: string;
    aiInsights?: string;
}

interface FeedbackListProps {
    feedbackList: Feedback[];
    onSeek: (timestamp: number) => void;
}

const FeedbackList: React.FC<FeedbackListProps> = ({ feedbackList, onSeek }) => {
    return (
        <div className="feedback-list">
            <h3>Feedback List</h3>
            <ul>
                {feedbackList.map((feedback) => (
                    <li key={feedback.id}>
                        {feedback.timestamp.toFixed(2)}s - {feedback.feedback}
                        {feedback.aiInsights && <p>AI: {feedback.aiInsights}</p>}
                        <button onClick={() => onSeek(feedback.timestamp)}>Go to</button>
                    </li>
                ))}
            </ul>
        </div>
    );
};

export default FeedbackList;