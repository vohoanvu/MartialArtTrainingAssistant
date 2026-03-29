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
        <div className="feedback-container p-4 rounded-md shadow-zen-md border border-[rgba(60,50,40,0.10)]">
            <div className="w-full">
                {/* Tab List */}
                <div className="flex space-x-2 border-b mb-4">
                    <button
                        onClick={() => setActiveTab('techniques')}
                        className={
                            `shadcn-ui-tab px-4 py-2 rounded-t-md font-medium transition-colors duration-150 focus:outline-none ` +
                            (activeTab === 'techniques'
                                ? 'bg-parchment-100 border-b-2 border-samurai-400 text-samurai-400 shadow-zen-sm'
                                : 'bg-parchment-200 text-slate-zen400 hover:text-ink-400 hover:bg-parchment-300')
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
                                ? 'bg-parchment-100 border-b-2 border-samurai-400 text-samurai-400 shadow-zen-sm'
                                : 'bg-parchment-200 text-slate-zen400 hover:text-ink-400 hover:bg-parchment-300')
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
                                ? 'bg-parchment-100 border-b-2 border-samurai-400 text-samurai-400 shadow-zen-sm'
                                : 'bg-parchment-200 text-slate-zen400 hover:text-ink-400 hover:bg-parchment-300')
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