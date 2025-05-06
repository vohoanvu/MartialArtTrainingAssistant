import { TechniqueDto } from '@/types/global';
import React from 'react';

interface TechniquesIdentifiedDisplayProps {
    techniques: TechniqueDto[];
    onSeek: (timestamp: number) => void;
    handleSaveToServer: () => void;
    onInputChange: (section: string, index: string | number, field: string | number, value: any) => void;
}

// const formatTimestamp = (seconds: number): string => {
//     const minutes = Math.floor(seconds / 60);
//     const remainingSeconds = Math.floor(seconds % 60);
//     return `${minutes.toString().padStart(2, '0')}:${remainingSeconds.toString().padStart(2, '0')}`;
// };

const TechniquesIdentifiedDisplay: React.FC<TechniquesIdentifiedDisplayProps> = ({ techniques, onSeek, handleSaveToServer, onInputChange }) => {
    const groupedTechniques = techniques.reduce((acc, tech) => {
        const scenario = tech.positionalScenario.name || 'Null Scenario';
        const type = tech.techniqueType.name || 'Null TechniqueType';

        if (!acc[scenario]) {
            acc[scenario] = {};
        }
        if (!acc[scenario][type]) {
            acc[scenario][type] = [];
        }
        acc[scenario][type].push(tech);
        return acc;
    }, {} as Record<string, Record<string, TechniqueDto[]>>);

    const handleEdit = (item: any) => {
        console.log("Editing:", item);
    };

    const handleSave = (section: any, index: string | number, name: any, value: any) => {
        //onInputChange('techniques', index, 'name', e.target.value)
        onInputChange(section, index, name, value);
        handleSaveToServer();
    }

    return (
        <div className="w-full space-y-2">
            {Object.entries(groupedTechniques).map(([scenario, types]) => (
                <details key={scenario} className="group border rounded-md overflow-hidden">
                    <summary className="cursor-pointer p-3 bg-gray-100 hover:bg-gray-200 font-medium list-none flex justify-between items-center">
                        Positional Scenario: {scenario}
                        <span className="text-gray-500 group-open:rotate-90 transform transition-transform duration-200">&#9656;</span> {/* Simple indicator */}
                    </summary>
                    <div className="p-3 border-t border-gray-200 space-y-2">
                        {Object.entries(types).map(([type, techList]) => (
                            <details key={type} className="group border rounded-md overflow-hidden">
                                <summary className="cursor-pointer p-2 bg-gray-50 hover:bg-gray-100 font-medium list-none flex justify-between items-center">
                                    Technique Type: {type}
                                    <span className="text-gray-500 group-open:rotate-90 transform transition-transform duration-200">&#9656;</span>
                                </summary>
                                <div className="p-3 border-t border-gray-100">
                                    <ul className="list-none p-0 space-y-4">
                                        {techList.map((tech) => (
                                            <li key={tech.id} className="border-l-4 border-blue-500 pl-4 py-2 bg-white rounded-r-md shadow-sm">
                                                <div className="flex justify-between items-center mb-1">
                                                    <strong className="text-base">Technique Name: {tech.name}</strong>
                                                    <button
                                                        className="px-2 py-1 text-xs font-medium text-gray-700 bg-gray-200 rounded hover:bg-gray-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500"
                                                        onClick={() => handleEdit(tech.id)}>Edit</button>
                                                </div>
                                                <div className="flex justify-between items-center mb-1">
                                                    <span
                                                        className="text-blue-600 hover:underline cursor-pointer text-sm"
                                                        onClick={() => onSeek(Number(tech.startTimestamp))}
                                                    >
                                                        Timestamp: {tech.startTimestamp} - {tech.endTimestamp}
                                                    </span>
                                                    <button
                                                        className="px-2 py-1 text-xs font-medium text-gray-700 bg-gray-200 rounded hover:bg-gray-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500"
                                                        onClick={() => handleEdit(tech.id)}>Edit</button>
                                                </div>
                                                <div className="flex justify-between items-start">
                                                    <p className="text-sm text-gray-700 mr-2">{tech.description}</p>
                                                    <button
                                                        className="px-2 py-1 text-xs font-medium text-gray-700 bg-gray-200 rounded hover:bg-gray-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500 flex-shrink-0"
                                                        onClick={() => handleEdit(tech.id)}>Edit</button>
                                                </div>
                                            </li>
                                        ))}
                                    </ul>
                                </div>
                            </details>
                        ))}
                    </div>
                </details>
            ))}
        </div>
    );
};

export default TechniquesIdentifiedDisplay;