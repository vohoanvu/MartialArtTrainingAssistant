import { TechniqueDto } from '@/types/global';
import React from 'react';

interface TechniqueGroupingCollapsibleProps {
    techniques: TechniqueDto[];
    onSeek: (timestamp: number) => void;
    handleSaveToServer: () => void;
    onInputChange: (section: string, index: string | number, field: string | number, value: any) => void;
}

const TechniqueGroupingCollapsible: React.FC<TechniqueGroupingCollapsibleProps> = ({ techniques, onSeek }) => {
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

    return (
        <div className="w-full space-y-2">
            {Object.entries(groupedTechniques).map(([scenario, types]) => (
                <details key={scenario} className="group border border-[rgba(60,50,40,0.10)] rounded-md overflow-hidden">
                    <summary className="cursor-pointer p-3 bg-parchment-200 hover:bg-parchment-300 font-medium list-none flex justify-between items-center">
                        Positional Scenario: {scenario}
                        <span className="text-slate-zen400 group-open:rotate-90 transform transition-transform duration-200">&#9656;</span> {/* Simple indicator */}
                    </summary>
                    <div className="p-3 border-t border-parchment-300 space-y-2">
                        {Object.entries(types).map(([type, techList]) => (
                            <details key={type} className="group border border-[rgba(60,50,40,0.10)] rounded-md overflow-hidden">
                                <summary className="cursor-pointer p-2 bg-parchment-100 hover:bg-parchment-200 font-medium list-none flex justify-between items-center">
                                    Technique Type: {type}
                                    <span className="text-slate-zen400 group-open:rotate-90 transform transition-transform duration-200">&#9656;</span>
                                </summary>
                                <div className="p-3 border-t border-parchment-200">
                                    <ul className="list-none p-0 space-y-4">
                                        {techList.map((tech) => (
                                            <li key={tech.id} className="border-l-4 border-samurai-400 pl-4 py-2 bg-parchment-50 rounded-r-md shadow-zen-sm">
                                                <div className="flex justify-between items-center mb-1">
                                                    <strong className="text-base">Technique Name: {tech.name}</strong>
                                                    <button
                                                        className="px-2 py-1 text-xs font-medium text-slate-zen400 bg-parchment-200 rounded hover:bg-parchment-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-samurai-400"
                                                        onClick={() => handleEdit(tech.id)}>Edit</button>
                                                </div>
                                                <div className="flex justify-between items-center mb-1">
                                                    <span
                                                        className="text-samurai-500 hover:underline cursor-pointer text-sm"
                                                        onClick={() => onSeek(Number(tech.startTimestamp))}
                                                    >
                                                        Timestamp: {tech.startTimestamp} - {tech.endTimestamp}
                                                    </span>
                                                    {/* <button
                                                        className="px-2 py-1 text-xs font-medium text-slate-zen400 bg-parchment-200 rounded hover:bg-parchment-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-samurai-400"
                                                        onClick={() => handleEdit(tech.id)}>Edit</button> */}
                                                </div>
                                                <div className="flex justify-between items-start">
                                                    <p className="text-sm text-slate-zen400 mr-2">{tech.description}</p>
                                                    <button
                                                        className="px-2 py-1 text-xs font-medium text-slate-zen400 bg-parchment-200 rounded hover:bg-parchment-300 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-samurai-400 flex-shrink-0"
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

export default TechniqueGroupingCollapsible;