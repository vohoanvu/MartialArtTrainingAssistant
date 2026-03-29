import { SuggestedDrill } from '@/types/global';
import React from 'react';

interface SuggestedDrillsDisplayProps {
    drills: SuggestedDrill[];
    handleSaveToServer: () => void;
    onInputChange: (section: string, index: string | number, field: string | number, value: any) => void;
}

const SuggestedDrillsDisplay: React.FC<SuggestedDrillsDisplayProps> = ({ drills }) => {
    const handleEdit = (item: any) => {
        console.log("Editing:", item);
        // Add actual edit logic here
    };

    return (
        <div className="space-y-4">
            {drills.map((drill, index) => (
                <div key={index} className="border border-[rgba(60,50,40,0.10)] rounded-md shadow-zen-sm overflow-hidden bg-parchment-50">
                    <div className="p-4 border-b border-[rgba(60,50,40,0.10)] bg-parchment-200">
                        <div className="flex justify-between items-center">
                            <h3 className="text-lg font-semibold font-serif">{drill.name}</h3>
                            <button
                                className="px-2 py-1 text-xs font-medium text-slate-zen400 bg-parchment-200 rounded hover:bg-parchment-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-samurai-400"
                                onClick={() => handleEdit(drill.name)}>Edit</button>
                        </div>
                    </div>
                    <div className="p-4 space-y-3 text-sm">
                        <div className="flex justify-between items-center">
                            <p><strong className="font-medium text-ink-400">Focus:</strong> {drill.focus}</p>
                            <button
                                className="px-2 py-1 text-xs font-medium text-slate-zen400 bg-parchment-200 rounded hover:bg-parchment-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-samurai-400"
                                onClick={() => handleEdit(drill.focus)}>Edit</button>
                        </div>
                        <div className="flex justify-between items-center">
                            <p><strong className="font-medium text-ink-400">Duration:</strong> {drill.duration}</p>
                            <button
                                className="px-2 py-1 text-xs font-medium text-slate-zen400 bg-parchment-200 rounded hover:bg-parchment-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-samurai-400"
                                onClick={() => handleEdit(drill.duration)}>Edit</button>
                        </div>
                        <div className="flex justify-between items-start">
                            <p className="mr-2"><strong className="font-medium text-ink-400">Description:</strong> {drill.description}</p>
                            <button
                                className="px-2 py-1 text-xs font-medium text-slate-zen400 bg-parchment-200 rounded hover:bg-parchment-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-samurai-400 flex-shrink-0"
                                onClick={() => handleEdit(drill.description)}>Edit</button>
                        </div>
                        <div className="flex justify-between items-center">
                            <p><strong className="font-medium text-ink-400">Related Technique:</strong> {drill.relatedTechniqueName}</p>
                             {/* No edit button for related technique */}
                        </div>
                    </div>
                </div>
            ))}
        </div>
    );
};

export default SuggestedDrillsDisplay;