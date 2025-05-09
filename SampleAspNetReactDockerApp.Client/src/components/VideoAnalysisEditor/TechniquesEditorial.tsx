import React, { useState, useEffect } from 'react';
import { AnalysisResultDto, TechniqueDto } from '@/types/global';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { ChevronDown, ChevronUp } from 'lucide-react';

interface TechniquesEditorialProps {
    analysisResultDto: AnalysisResultDto;
    onSeek: (timestamp: number) => void;
    handleSaveChanges: (updatedFeedbackData: AnalysisResultDto) => Promise<void>;
    selectedSegment?: { start: string; end: string } | null;
}

const techniqueTypes = [
    'Grip Fighting',
    'Guard Pull',
    'Takedown Defense',
    'Escape',
    'Transition/Escape',
    'Takedown Defense (Unsuccessful)',
];

const positionalScenarios = [
    'Standing',
    'Standing to Guard',
    'Bottom Side Control',
    'Bottom Mount',
    'Ground Scramble to Standing',
];

export const TechniquesEditorial: React.FC<TechniquesEditorialProps> = ({
    analysisResultDto,
    onSeek,
    handleSaveChanges,
    selectedSegment,
}) => {
    const [techniques, setTechniques] = useState<TechniqueDto[]>(analysisResultDto.techniques ?? []);
    const [showCreateForm, setShowCreateForm] = useState(false);
    const [newTechnique, setNewTechnique] = useState<TechniqueDto>({
        name: '',
        description: '',
        techniqueType: {
            name: '',
            positionalScenario: ''
        },
        positionalScenario: { name: '' },
        startTimestamp: '',
        endTimestamp: '',
    });
    const [expandedTechniques, setExpandedTechniques] = useState<boolean[]>([]);

    useEffect(() => {
        setTechniques(analysisResultDto.techniques ?? []);
        setExpandedTechniques(new Array(analysisResultDto.techniques?.length ?? 0).fill(false));
    }, [analysisResultDto]);

    useEffect(() => {
        if (selectedSegment) {
            setNewTechnique((prev) => ({
                ...prev,
                startTimestamp: selectedSegment.start,
                endTimestamp: selectedSegment.end,
            }));
        }
    }, [selectedSegment]);

    const toggleTechniqueDetails = (index: number) => {
        setExpandedTechniques((prev) =>
            prev.map((expanded, i) => (i === index ? !expanded : expanded))
        );
    };

    const handleTechniqueChange = (
        index: number,
        field: keyof TechniqueDto,
        value: string
    ) => {
        const updatedTechniques = [...techniques];
        if (field === 'techniqueType') {
            updatedTechniques[index].techniqueType = { 
                name: value, 
                positionalScenario: updatedTechniques[index].techniqueType.positionalScenario || '' 
            };
        } else if (field === 'positionalScenario') {
            updatedTechniques[index].positionalScenario = { name: value };
        } else {
            (updatedTechniques[index] as any)[field] = value;
        }
        setTechniques(updatedTechniques);
    };

    const handleNewTechniqueChange = (
        field: keyof TechniqueDto,
        value: string
    ) => {
        if (field === 'techniqueType' || field === 'positionalScenario') {
            setNewTechnique((prev) => ({ ...prev, [field]: { name: value } }));
        } else {
            setNewTechnique((prev) => ({ ...prev, [field]: value }));
        }
    };

    const addTechnique = () => {
        setTechniques([...techniques, { ...newTechnique }]);
        setNewTechnique({
            name: '',
            description: '',
            techniqueType: {
                name: '',
                positionalScenario: ''
            },
            positionalScenario: { name: '' },
            startTimestamp: '',
            endTimestamp: '',
        });
        setShowCreateForm(false);
    };

    const deleteTechnique = (index: number) => {
        setTechniques(techniques.filter((_, i) => i !== index));
        setExpandedTechniques(expandedTechniques.filter((_, i) => i !== index));
    };

    const saveChanges = async () => {
        const updatedFeedbackData: AnalysisResultDto = {
            ...analysisResultDto,
            techniques,
        };
        await handleSaveChanges(updatedFeedbackData);
    };

    return (
        <div className="space-y-4">
            <Button
                onClick={() => setShowCreateForm(!showCreateForm)}
                className=""
                variant="default"
            >
                <span className="text-xl mr-2">+</span> Create New Technique
            </Button>

            {showCreateForm && (
                <div className="p-4 rounded-md space-y-4">
                    <p className="text-sm">
                        Please select a video segment from the video player timeline to set the
                        timestamps for this technique.
                    </p>
                    <div>
                        <label className="block text-sm font-medium">
                            Technique Name
                        </label>
                        <Input
                            value={newTechnique.name}
                            onChange={(e) => handleNewTechniqueChange('name', e.target.value)}
                            placeholder="Enter technique name"
                            className="mt-1"
                        />
                    </div>
                    <div className="flex space-x-4">
                        <div className="w-1/2">
                            <label className="block text-sm font-medium">
                                Start Timestamp
                            </label>
                            <Input
                                value={newTechnique.startTimestamp || ''}
                                readOnly
                                placeholder="e.g., 00:00:00"
                                className="mt-1"
                            />
                        </div>
                        <div className="w-1/2">
                            <label className="block text-sm font-medium">
                                End Timestamp
                            </label>
                            <Input
                                value={newTechnique.endTimestamp || ''}
                                readOnly
                                placeholder="e.g., 00:00:00"
                                className="mt-1"
                            />
                        </div>
                    </div>
                    <div>
                        <label className="block text-sm font-medium">
                            Technique Type
                        </label>
                        <Select
                            value={newTechnique.techniqueType.name}
                            onValueChange={(value) =>
                                handleNewTechniqueChange('techniqueType', value)
                            }
                        >
                            <SelectTrigger className="mt-1">
                                <SelectValue placeholder="Select Technique Type" />
                            </SelectTrigger>
                            <SelectContent>
                                {techniqueTypes.map((type) => (
                                    <SelectItem key={type} value={type}>
                                        {type}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>
                    <div>
                        <label className="block text-sm font-medium">
                            Positional Scenario
                        </label>
                        <Select
                            value={newTechnique.positionalScenario.name}
                            onValueChange={(value) =>
                                handleNewTechniqueChange('positionalScenario', value)
                            }
                        >
                            <SelectTrigger className="mt-1">
                                <SelectValue placeholder="Select Positional Scenario" />
                            </SelectTrigger>
                            <SelectContent>
                                {positionalScenarios.map((scenario) => (
                                    <SelectItem key={scenario} value={scenario}>
                                        {scenario}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>
                    <div>
                        <label className="block text-sm font-medium">
                            Description
                        </label>
                        <Textarea
                            value={newTechnique.description ?? ''}
                            onChange={(e) =>
                                handleNewTechniqueChange('description', e.target.value)
                            }
                            placeholder="Enter description"
                            className="mt-1"
                        />
                    </div>
                    <div className="flex space-x-2">
                        <Button
                            onClick={addTechnique}
                            variant="default"
                            className=""
                        >
                            Add Technique
                        </Button>
                        <Button
                            onClick={() => setShowCreateForm(false)}
                            className=""
                            variant="outline"
                        >
                            Cancel
                        </Button>
                    </div>
                </div>
            )}

            {techniques.map((technique, index) => (
                <div
                    key={index}
                    className="border-b pb-4"
                >
                    <div
                        className="flex justify-between items-center cursor-pointer p-2"
                        onClick={() => toggleTechniqueDetails(index)}
                    >
                        <div>
                            <p>
                                <strong>Technique Name:</strong> {technique.name}
                            </p>
                            <p>
                                <strong>Timestamp:</strong>{' '}
                                <span className="px-1 rounded">
                                    {technique.startTimestamp}
                                </span>{' '}
                                -{' '}
                                <span className="px-1 rounded">
                                    {technique.endTimestamp}
                                </span>
                            </p>
                        </div>
                        {expandedTechniques[index] ? (
                            <ChevronUp className="text-blue-500" />
                        ) : (
                            <ChevronDown className="text-blue-500" />
                        )}
                    </div>
                    {expandedTechniques[index] && (
                        <div className="p-2 space-y-2">
                            <div>
                                <label className="block text-sm font-medium">
                                    Technique Type
                                </label>
                                <Select
                                    value={technique.techniqueType.name}
                                    onValueChange={(value) =>
                                        handleTechniqueChange(index, 'techniqueType', value)
                                    }
                                >
                                    <SelectTrigger className="mt-1">
                                        <SelectValue />
                                    </SelectTrigger>
                                    <SelectContent>
                                        {techniqueTypes.map((type) => (
                                            <SelectItem key={type} value={type}>
                                                {type}
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                            </div>
                            <div>
                                <label className="block text-sm font-medium">
                                    Positional Scenario
                                </label>
                                <Select
                                    value={technique.positionalScenario.name}
                                    onValueChange={(value) =>
                                        handleTechniqueChange(index, 'positionalScenario', value)
                                    }
                                >
                                    <SelectTrigger className="mt-1">
                                        <SelectValue />
                                    </SelectTrigger>
                                    <SelectContent>
                                        {positionalScenarios.map((scenario) => (
                                            <SelectItem key={scenario} value={scenario}>
                                                {scenario}
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                            </div>
                            <div>
                                <label className="block text-sm font-medium">
                                    Description
                                </label>
                                <Textarea
                                    value={technique.description ?? ''}
                                    onChange={(e) =>
                                        handleTechniqueChange(index, 'description', e.target.value)
                                    }
                                    className="mt-1"
                                />
                            </div>
                            <Button
                                onClick={() => onSeek(Number(technique.startTimestamp))}
                                className="mr-2"
                                variant="secondary"
                                size='sm'
                            >
                                Seek to Technique
                            </Button>
                            <Button
                                onClick={() => deleteTechnique(index)}
                                className=""
                                variant="destructive"
                                size='sm'
                            >
                                Delete Technique
                            </Button>
                        </div>
                    )}
                </div>
            ))}

            <Button
                onClick={saveChanges}
                className="mt-4"
                variant="default"
                size='lg'
            >
                Save Changes
            </Button>
        </div>
    );
};