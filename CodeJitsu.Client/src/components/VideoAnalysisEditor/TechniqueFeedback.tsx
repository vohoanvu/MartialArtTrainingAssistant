import React, { useState } from 'react';
import { AnalysisResultDto } from '@/types/global';
import { TechniquesEditorial } from './TechniquesEditorial';
import { DrillsEditorial } from './DrillsEditorials';
import { OverallAnalysisEditorial } from './OverallAnalysisEditorial';

interface TechniqueFeedbackProps {
    feedbackData: AnalysisResultDto | null;
    onSeek: (timestamp: string) => void;
    saveChanges: (updatedFeedbackData: AnalysisResultDto) => Promise<void>;
    onInputChange: (section: string, index: string | number, field: string | number, value: any) => void;
    selectedSegment?: { start: string; end: string } | null;
    isAnalysisSaving?: boolean;
}

type ActiveTab = 'techniques' | 'drills' | 'analysis';

const TechniqueFeedback: React.FC<TechniqueFeedbackProps> = ({
    feedbackData,
    onSeek,
    saveChanges,
    selectedSegment,
    isAnalysisSaving
}) => {
    const [activeTab, setActiveTab] = useState<ActiveTab>('techniques');

    if (!feedbackData) {
        return <div className="p-4">Loading feedback data...</div>;
    }

    return (
        <div className="feedback-container p-4 rounded-md shadow-md border">
            <div className="w-full">
                {/* Tab List */}
                <div className="flex space-x-2 border-b mb-4">
                    <button
                        onClick={() => setActiveTab('techniques')}
                        className={
                            `shadcn-ui-tab px-4 py-2 rounded-t-md font-medium transition-colors duration-150 focus:outline-none ` +
                            (activeTab === 'techniques'
                                ? 'bg-background border-b-2 border-primary text-primary shadow'
                                : 'bg-muted text-muted-foreground hover:text-foreground hover:bg-accent')
                        }
                        type="button"
                    >
                        Identified Techniques
                    </button>
                    <button
                        onClick={() => setActiveTab('drills')}
                        className={
                            `shadcn-ui-tab px-4 py-2 rounded-t-md font-medium transition-colors duration-150 focus:outline-none ` +
                            (activeTab === 'drills'
                                ? 'bg-background border-b-2 border-primary text-primary shadow'
                                : 'bg-muted text-muted-foreground hover:text-foreground hover:bg-accent')
                        }
                        type="button"
                    >
                        Suggested Drills
                    </button>
                    <button
                        onClick={() => setActiveTab('analysis')}
                        className={
                            `shadcn-ui-tab px-4 py-2 rounded-t-md font-medium transition-colors duration-150 focus:outline-none ` +
                            (activeTab === 'analysis'
                                ? 'bg-background border-b-2 border-primary text-primary shadow'
                                : 'bg-muted text-muted-foreground hover:text-foreground hover:bg-accent')
                        }
                        type="button"
                    >
                        Overall Analysis
                    </button>
                </div>

                {/* Tab Content */}
                <div className="mt-4">
                    {activeTab === 'techniques' && (
                        <TechniquesEditorial
                            analysisResultDto={feedbackData}
                            onSeek={onSeek}
                            handleSaveChanges={saveChanges}
                            selectedSegment={selectedSegment}
                            isAnalysisSaving={isAnalysisSaving}
                        />
                    )}
                    {activeTab === 'drills' && (
                        <DrillsEditorial
                            analysisResultDto={feedbackData}
                            handleSaveChanges={saveChanges}
                            isAnalysisSaving={isAnalysisSaving}
                        />
                    )}
                    {activeTab === 'analysis' && (
                        <OverallAnalysisEditorial
                            analysisResultDto={feedbackData}
                            handleSaveChanges={saveChanges}
                            isAnalysisSaving={isAnalysisSaving}
                        />
                    )}
                </div>
            </div>
        </div>
    );
};

export default TechniqueFeedback;