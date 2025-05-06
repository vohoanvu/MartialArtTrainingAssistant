import React, { useState } from 'react';
import TechniquesIdentifiedDisplay from './IdentifiedTechniques';
import SuggestedDrillsDisplay from './SuggestedDrills';
import OverallAnalysisDisplay from './OverallDescription';
import { AnalysisResultDto } from '@/types/global';

interface TechniqueFeedbackProps {
    feedbackData: AnalysisResultDto | null;
    onSeek: (timestamp: number) => void;
    handleSaveToServer: () => void;
    onInputChange: (section: string, index: string | number, field: string | number, value: any) => void;
}

type ActiveTab = 'techniques' | 'drills' | 'analysis';

const TechniqueFeedback: React.FC<TechniqueFeedbackProps> = ({ 
    feedbackData, 
    onSeek, 
    handleSaveToServer, 
    onInputChange,
}) => {
    const [activeTab, setActiveTab] = useState<ActiveTab>('techniques');

    if (!feedbackData) {
        return <div className="p-4">Loading feedback data...</div>;
    }

    const getTabClass = (tabName: ActiveTab): string => {
        return `py-2 px-4 text-center font-medium cursor-pointer border-b-2 transition-colors duration-150 ${
            activeTab === tabName
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
        }`;
    };

    return (
        <div className="feedback-container p-4 rounded-md shadow-md border bg-gray-50">
             <div className="w-full">
                {/* Tab List */}
                <div className="grid grid-cols-3 border-b border-gray-200 mb-4">
                    <button
                        onClick={() => setActiveTab('techniques')}
                        className={getTabClass('techniques')}
                    >
                        Identified Techniques
                    </button>
                    <button
                        onClick={() => setActiveTab('drills')}
                        className={getTabClass('drills')}
                    >
                        Suggested Drills
                    </button>
                    <button
                        onClick={() => setActiveTab('analysis')}
                        className={getTabClass('analysis')}
                    >
                        Overall Analysis
                    </button>
                </div>

                {/* Tab Content */}
                <div className="mt-4">
                    {activeTab === 'techniques' && (
                        <TechniquesIdentifiedDisplay
                            techniques={feedbackData.techniques || []}
                            onSeek={onSeek}
                            onInputChange={onInputChange}
                            handleSaveToServer={handleSaveToServer}
                        />
                    )}
                    {activeTab === 'drills' && (
                        <SuggestedDrillsDisplay 
                            drills={feedbackData.drills || []} 
                            onInputChange={onInputChange}
                            handleSaveToServer={handleSaveToServer}
                        />
                    )}
                    {activeTab === 'analysis' && (
                        <OverallAnalysisDisplay
                            overallDescription={feedbackData.overallDescription || ''}
                            strengths={feedbackData.strengths || []}
                            areasForImprovement={feedbackData.areasForImprovement || []}
                            onInputChange={onInputChange}
                            handleSaveToServer={handleSaveToServer}
                        />
                    )}
                </div>
            </div>
        </div>
    );
};

export default TechniqueFeedback;

