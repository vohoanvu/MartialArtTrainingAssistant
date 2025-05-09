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
      <div className="p-4 bg-destructive/10 text-destructive rounded-md">
        Invalid JSON format. Please check the input data.
      </div>
    );
  }

  return (
    <div className="p-6 bg-background border border-border rounded-md shadow-md">
      <h2 className="text-2xl font-bold mb-4 text-foreground">AI Analysis Results (JSON View)</h2>
      <div className="bg-muted p-4 rounded-md overflow-auto">
        <pre className="text-sm text-foreground whitespace-pre-wrap">
          {JSON.stringify(parsedData, null, 2)}
        </pre>
      </div>
    </div>
  );
};

export default AiAnalysisResults;