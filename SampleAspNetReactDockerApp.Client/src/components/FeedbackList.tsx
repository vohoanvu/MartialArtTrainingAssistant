import React from 'react';
import { Button } from '@/components/ui/button';
import { Feedback } from '@/pages/VideoReview';

interface FeedbackListProps {
    feedbackList: Feedback[];
    onSeek: (timestamp: number) => void;
}

const FeedbackList: React.FC<FeedbackListProps> = ({ feedbackList, onSeek }) => {
    const sortedFeedbackList = [...feedbackList].sort((a, b) => a.fromTimestamp - b.fromTimestamp);

    return (
        <div className="feedback-list p-4 rounded-md shadow-md">
            <h3 className="text-lg font-semibold mb-2">Feedback List</h3>
            <ul className="list-none p-0">
                {sortedFeedbackList.map((feedback) => (
                    <li key={feedback.id} className="grid grid-cols-3 gap-4 items-center mb-4 p-2 border rounded-md">
                        <div className="col-span-1">
                            <span className="font-medium text-blue-500">{feedback.fromTimestamp.toFixed(2)}s - {feedback.toTimestamp.toFixed(2)}s</span>
                        </div>
                        <div className="col-span-1">
                            <span>{feedback.feedback.substring(0, 50)}...</span>
                        </div>
                        <div className="col-span-1 flex justify-end gap-2">
                            <Button variant="secondary" size="sm" onClick={() => onSeek(feedback.fromTimestamp)}>Go to</Button>
                            <Button variant="outline" size="sm">Edit</Button>
                            <Button variant="destructive" size="sm">Delete</Button>
                        </div>
                    </li>
                ))}
            </ul>
        </div>
    );
};

export default FeedbackList;