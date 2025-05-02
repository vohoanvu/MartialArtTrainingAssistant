import React from 'react';

interface AiAnalysisResultsProps {
  analysisJson: string;
}

const AiAnalysisResults: React.FC<AiAnalysisResultsProps> = ({ analysisJson }) => {
  let parsedData: object | null = null;

  try {
    parsedData = JSON.parse(analysisJson);
  } catch (error) {
    return (
      <div className="p-4 bg-red-100 text-red-700 rounded-md">
        Invalid JSON format. Please check the input data.
      </div>
    );
  }

  return (
    <div className="p-6 bg-white border rounded-md shadow-md">
      <h2 className="text-2xl font-bold mb-4">AI Analysis Results (JSON View)</h2>
      <div className="bg-gray-100 p-4 rounded-md overflow-auto">
        <pre className="text-sm text-gray-800 whitespace-pre-wrap">
          {JSON.stringify(parsedData, null, 2)}
        </pre>
      </div>
    </div>
  );
};

export default AiAnalysisResults;