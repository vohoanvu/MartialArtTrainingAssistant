import { AreaForImprovement, Strength } from '@/types/global';
import React from 'react';

interface OverallAnalysisDisplayProps {
    overallDescription: string;
    strengths: Strength[];
    areasForImprovement: AreaForImprovement[];
    handleSaveToServer: () => void;
    onInputChange: (section: string, index: string | number, field: string | number, value: any) => void;
}

const OverallAnalysisDisplay: React.FC<OverallAnalysisDisplayProps> = ({
    overallDescription,
    strengths,
    areasForImprovement
}) => {

    const handleEdit = (item: any) => {
        console.log("Editing:", item);
        // Add actual edit logic here
    };

    return (
        <div className="space-y-4">
            {/* Overall Description Section */}
            <div className="border border-[rgba(60,50,40,0.10)] rounded-md shadow-zen-sm overflow-hidden bg-parchment-50">
                <div className="p-4 border-b border-[rgba(60,50,40,0.10)] bg-parchment-200">
                    <div className="flex justify-between items-center">
                        <h3 className="text-lg font-semibold font-serif">Overall Description</h3>
                        <button
                            className="px-2 py-1 text-xs font-medium text-slate-zen400 bg-parchment-200 rounded hover:bg-parchment-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-samurai-400"
                            onClick={() => handleEdit(overallDescription)}>Edit</button>
                    </div>
                </div>
                <div className="p-4">
                    <p className="text-sm text-slate-zen400">{overallDescription}</p>
                </div>
            </div>

            {/* Strengths Section */}
            <div className="border border-[rgba(60,50,40,0.10)] rounded-md shadow-zen-sm overflow-hidden bg-parchment-50">
                <div className="p-4 border-b border-[rgba(60,50,40,0.10)] bg-parchment-200">
                    <h3 className="text-lg font-semibold font-serif">Strengths</h3>
                </div>
                <div className="p-4">
                    <ul className="list-disc pl-5 space-y-3 text-sm">
                        {strengths.map((strength, index) => (
                            <li key={index}>
                                <div className="flex justify-between items-start">
                                    <span className="mr-2">
                                        {strength.description}
                                    </span>
                                    <button
                                        className="px-2 py-1 text-xs font-medium text-slate-zen400 bg-parchment-200 rounded hover:bg-parchment-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-samurai-400 flex-shrink-0"
                                        onClick={() => handleEdit(strength)}>Edit</button>
                                </div>
                            </li>
                        ))}
                    </ul>
                </div>
            </div>

            {/* Areas for Improvement Section */}
            <div className="border border-[rgba(60,50,40,0.10)] rounded-md shadow-zen-sm overflow-hidden bg-parchment-50">
                <div className="p-4 border-b border-[rgba(60,50,40,0.10)] bg-parchment-200">
                    <h3 className="text-lg font-semibold font-serif">Areas for Improvement</h3>
                </div>
                <div className="p-4">
                    <ul className="list-disc pl-5 space-y-3 text-sm">
                        {areasForImprovement.map((area, index) => (
                            <li key={index}>
                                <div className="flex justify-between items-start">
                                    <span className="mr-2">
                                        {area.description}
                                    </span>
                                    <button
                                        className="px-2 py-1 text-xs font-medium text-slate-zen400 bg-parchment-200 rounded hover:bg-parchment-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-samurai-400 flex-shrink-0"
                                        onClick={() => handleEdit(area)}>Edit</button>
                                </div>
                            </li>
                        ))}
                    </ul>
                </div>
            </div>
        </div>
    );
};

export default OverallAnalysisDisplay;