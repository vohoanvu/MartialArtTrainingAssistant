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
            <div className="border rounded-md shadow-sm overflow-hidden bg-white">
                <div className="p-4 border-b bg-gray-50">
                    <div className="flex justify-between items-center">
                        <h3 className="text-lg font-semibold">Overall Description</h3>
                        <button
                            className="px-2 py-1 text-xs font-medium text-gray-700 bg-gray-200 rounded hover:bg-gray-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500"
                            onClick={() => handleEdit(overallDescription)}>Edit</button>
                    </div>
                </div>
                <div className="p-4">
                    <p className="text-sm text-gray-700">{overallDescription}</p>
                </div>
            </div>

            {/* Strengths Section */}
            <div className="border rounded-md shadow-sm overflow-hidden bg-white">
                <div className="p-4 border-b bg-gray-50">
                    <h3 className="text-lg font-semibold">Strengths</h3>
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
                                        className="px-2 py-1 text-xs font-medium text-gray-700 bg-gray-200 rounded hover:bg-gray-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500 flex-shrink-0"
                                        onClick={() => handleEdit(strength)}>Edit</button>
                                </div>
                            </li>
                        ))}
                    </ul>
                </div>
            </div>

            {/* Areas for Improvement Section */}
            <div className="border rounded-md shadow-sm overflow-hidden bg-white">
                <div className="p-4 border-b bg-gray-50">
                    <h3 className="text-lg font-semibold">Areas for Improvement</h3>
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
                                        className="px-2 py-1 text-xs font-medium text-gray-700 bg-gray-200 rounded hover:bg-gray-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500 flex-shrink-0"
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