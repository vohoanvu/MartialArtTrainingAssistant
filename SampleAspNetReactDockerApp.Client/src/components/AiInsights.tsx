export interface Feedback {
    id: number;
    timestamp: number;
    feedbackType: string;
    feedbackText: string;
}

interface AIInsightsProps {
    aiFeedback: Feedback[];
    onIncorporate: (feedback: Feedback) => void;
}

const AIInsights: React.FC<AIInsightsProps> = ({ aiFeedback, onIncorporate }) => (
    <div className="bg-blue-100 p-4">
        <h3>AI Insights</h3>
        {aiFeedback.map(f => (
            <div key={f.id}>
                <p>{f.timestamp.toFixed(2)}s - {f.feedbackType}</p>
                <p>{f.feedbackText}</p>
                <button onClick={() => onIncorporate(f)}>Incorporate</button>
            </div>
        ))}
    </div>
);

export default AIInsights;