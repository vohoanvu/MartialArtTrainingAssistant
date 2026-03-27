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
                <div key={index} className="border rounded-md shadow-sm overflow-hidden bg-white">
                    <div className="p-4 border-b bg-gray-50">
                        <div className="flex justify-between items-center">
                            <h3 className="text-lg font-semibold">{drill.name}</h3>
                            <button
                                className="px-2 py-1 text-xs font-medium text-gray-700 bg-gray-200 rounded hover:bg-gray-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500"
                                onClick={() => handleEdit(drill.name)}>Edit</button>
                        </div>
                    </div>
                    <div className="p-4 space-y-3 text-sm">
                        <div className="flex justify-between items-center">
                            <p><strong className="font-medium text-gray-800">Focus:</strong> {drill.focus}</p>
                            <button
                                className="px-2 py-1 text-xs font-medium text-gray-700 bg-gray-200 rounded hover:bg-gray-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500"
                                onClick={() => handleEdit(drill.focus)}>Edit</button>
                        </div>
                        <div className="flex justify-between items-center">
                            <p><strong className="font-medium text-gray-800">Duration:</strong> {drill.duration}</p>
                            <button
                                className="px-2 py-1 text-xs font-medium text-gray-700 bg-gray-200 rounded hover:bg-gray-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500"
                                onClick={() => handleEdit(drill.duration)}>Edit</button>
                        </div>
                        <div className="flex justify-between items-start">
                            <p className="mr-2"><strong className="font-medium text-gray-800">Description:</strong> {drill.description}</p>
                            <button
                                className="px-2 py-1 text-xs font-medium text-gray-700 bg-gray-200 rounded hover:bg-gray-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500 flex-shrink-0"
                                onClick={() => handleEdit(drill.description)}>Edit</button>
                        </div>
                        <div className="flex justify-between items-center">
                            <p><strong className="font-medium text-gray-800">Related Technique:</strong> {drill.relatedTechniqueName}</p>
                             {/* No edit button for related technique */}
                        </div>
                    </div>
                </div>
            ))}
        </div>
    );
};

export default SuggestedDrillsDisplay;