import React from 'react';

interface Technique {
  name: string;
  description: string;
  timestamp: string;
}

interface Drill {
  name: string;
  description: string;
  focus: string;
}

interface AnalysisData {
  techniques_identified: Technique[];
  textual_description: string;
  strengths: string[];
  areas_for_improvement: string[];
  suggested_drills: Drill[];
}

interface AiAnalysisResultsProps {
  analysisJson: string;
}

const AiAnalysisResults: React.FC<AiAnalysisResultsProps> = ({ analysisJson }) => {
    let data: AnalysisData | null = null;

    try {
        data = JSON.parse(analysisJson);
    } catch (error) {
        return (
            <div className="p-4 bg-red-100 text-red-700 rounded-md">
                Invalid analysis data format.
            </div>
        );
    }
    
    if (!data) {
        return (
            <div className="p-4 bg-red-100 text-red-700 rounded-md">
                Invalid analysis data format.
            </div>
        );
    }

    return (
        <div className="p-6 bg-white border rounded-md shadow-md">
            <h2 className="text-2xl font-bold mb-4">AI Analysis Results</h2>
            
            <section className="mb-6">
                <h3 className="text-xl font-semibold mb-1">Techniques Identified</h3>
                {data.techniques_identified && data.techniques_identified.length > 0 ? (
                    <ul className="list-disc pl-6">
                        {data.techniques_identified.map((technique, index) => (
                        <li key={index}>
                            <span className="font-medium">{technique.name}</span> ({technique.timestamp}): {technique.description}
                        </li>
                        ))}
                    </ul>
                ) : (
                    <p>No techniques identified.</p>
                )}
            </section>

            <section className="mb-6">
                <h3 className="text-xl font-semibold mb-1">Description</h3>
                <p>{data.textual_description}</p>
            </section>

            <section className="mb-6">
                <h3 className="text-xl font-semibold mb-1">Strengths</h3>
                {data.strengths && data.strengths.length > 0 ? (
                    <ul className="list-disc pl-6">
                        {data.strengths.map((strength, index) => (
                        <li key={index}>{strength}</li>
                        ))}
                    </ul>
                ) : (
                    <p>No strengths provided.</p>
                )}
            </section>

            <section className="mb-6">
                <h3 className="text-xl font-semibold mb-1">Areas for Improvement</h3>
                {data.areas_for_improvement && data.areas_for_improvement.length > 0 ? (
                    <ul className="list-disc pl-6">
                        {data.areas_for_improvement.map((area, index) => (
                        <li key={index}>{area}</li>
                        ))}
                    </ul>
                ) : (
                    <p>No areas for improvement provided.</p>
                )}
            </section>

            <section>
                <h3 className="text-xl font-semibold mb-1">Suggested Drills</h3>
                {data.suggested_drills && data.suggested_drills.length > 0 ? (
                    <ul className="list-disc pl-6">
                        {data.suggested_drills.map((drill, index) => (
                        <li key={index}>
                            <span className="font-medium">{drill.name}:</span> {drill.description}
                            <br />
                            <span className="italic text-sm">Focus: {drill.focus}</span>
                        </li>
                        ))}
                    </ul>
                ) : (
                    <p>No suggested drills provided.</p>
                )}
            </section>
        </div>
    );
};

export default AiAnalysisResults;