import React, { useState, useEffect } from 'react';
import { AnalysisResultDto, SuggestedDrill } from '@/types/global';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { ChevronDown, ChevronUp } from 'lucide-react';

interface DrillsEditorialProps {
    analysisResultDto: AnalysisResultDto;
    handleSaveChanges: (updatedFeedbackData: AnalysisResultDto) => Promise<void>;
}

export const DrillsEditorial: React.FC<DrillsEditorialProps> = ({
    analysisResultDto,
    handleSaveChanges,
}) => {
    const [drills, setDrills] = useState<SuggestedDrill[]>(analysisResultDto.drills ?? []);
    const [showCreateForm, setShowCreateForm] = useState(false);
    const [newDrill, setNewDrill] = useState<SuggestedDrill>({
        name: '',
        focus: '',
        duration: '',
        description: '',
        relatedTechniqueName: '',
    });
    const [expandedDrills, setExpandedDrills] = useState<boolean[]>([]);

    useEffect(() => {
        setDrills(analysisResultDto.drills ?? []);
        setExpandedDrills(new Array(analysisResultDto.drills?.length ?? 0).fill(false));
    }, [analysisResultDto]);

    const toggleDrillDetails = (index: number) => {
        setExpandedDrills((prev) =>
            prev.map((expanded, i) => (i === index ? !expanded : expanded))
        );
    };

    const handleDrillChange = (
        index: number,
        field: keyof SuggestedDrill,
        value: string
    ) => {
        const updatedDrills = [...drills];
        updatedDrills[index] = {
            ...updatedDrills[index],
            [field]: field === 'focus' ? (value === '' ? null : value) : value,
        };
        setDrills(updatedDrills);
    };

    const handleNewDrillChange = (
        field: keyof SuggestedDrill,
        value: string
    ) => {
        setNewDrill((prev) => ({ ...prev, [field]: value }));
    };

    const addDrill = () => {
        setDrills([...drills, { ...newDrill }]);
        setNewDrill({
            name: '',
            focus: '',
            duration: '',
            description: '',
            relatedTechniqueName: '',
        });
        setShowCreateForm(false);
    };

    const deleteDrill = (index: number) => {
        setDrills(drills.filter((_, i) => i !== index));
        setExpandedDrills(expandedDrills.filter((_, i) => i !== index));
    };

    const saveChanges = async () => {
        const updatedFeedbackData: AnalysisResultDto = {
            drills,
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
                <span className="text-xl mr-2">+</span> Create New Drill
            </Button>

            {showCreateForm && (
                <div className="p-4 bg-background rounded-md space-y-4 border border-border">
                    <div>
                        <label className="block text-sm font-medium">
                            Drill Name
                        </label>
                        <Input
                            value={newDrill.name}
                            onChange={(e) => handleNewDrillChange('name', e.target.value)}
                            placeholder="Enter drill name"
                            className="mt-1"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-medium">
                            Focus
                        </label>
                        <Input
                            value={newDrill.focus || ''}
                            onChange={(e) => handleNewDrillChange('focus', e.target.value)}
                            placeholder="Enter focus (e.g., Base, Posture)"
                            className="mt-1"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-medium">
                            Duration
                        </label>
                        <Input
                            value={newDrill.duration}
                            onChange={(e) => handleNewDrillChange('duration', e.target.value)}
                            placeholder="Enter duration (e.g., 3-5 minutes)"
                            className="mt-1"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-medium">
                            Description
                        </label>
                        <Textarea
                            value={newDrill.description}
                            onChange={(e) => handleNewDrillChange('description', e.target.value)}
                            placeholder="Enter description"
                            className="mt-1"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-medium">
                            Related Technique
                        </label>
                        <Select
                            value={newDrill.relatedTechniqueName === '' ? '__none__' : newDrill.relatedTechniqueName}
                            onValueChange={(value) =>
                                handleNewDrillChange('relatedTechniqueName', value === '__none__' ? '' : value)
                            }
                        >
                            <SelectTrigger className="mt-1">
                                <SelectValue placeholder="Select related technique (optional)" />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="__none__">None</SelectItem>
                                {analysisResultDto.techniques?.map((tech) => (
                                    <SelectItem key={tech.name} value={tech.name}>
                                        {tech.name}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>
                    <div className="flex space-x-2">
                        <Button
                            onClick={addDrill}
                            className="bg-gray-800 text-white hover:bg-gray-700"
                        >
                            Add Drill
                        </Button>
                        <Button
                            onClick={() => setShowCreateForm(false)}
                            className="bg-gray-600 text-white hover:bg-gray-500"
                        >
                            Cancel
                        </Button>
                    </div>
                </div>
            )}

            {drills.map((drill, index) => (
                <div
                    key={index}
                    className="border-b border-border pb-4"
                >
                    <div
                        className="flex justify-between items-center cursor-pointer p-2 hover:bg-accent/40"
                        onClick={() => toggleDrillDetails(index)}
                    >
                        <p>
                            <strong>Drill Name:</strong> <span className="text-foreground">{drill.name}</span>
                        </p>
                        {expandedDrills[index] ? (
                            <ChevronUp className="text-primary" />
                        ) : (
                            <ChevronDown className="text-primary" />
                        )}
                    </div>
                    {expandedDrills[index] && (
                        <div className="p-2 space-y-2 bg-background rounded-md border border-border">
                            <div>
                                <label className="block text-sm font-medium">
                                    Focus
                                </label>
                                <Input
                                    value={drill.focus || ''}
                                    onChange={(e) =>
                                        handleDrillChange(index, 'focus', e.target.value)
                                    }
                                    className="mt-1"
                                />
                            </div>
                            <div>
                                <label className="block text-sm font-medium">
                                    Duration
                                </label>
                                <Input
                                    value={drill.duration}
                                    onChange={(e) =>
                                        handleDrillChange(index, 'duration', e.target.value)
                                    }
                                    className="mt-1"
                                />
                            </div>
                            <div>
                                <label className="block text-sm font-medium">
                                    Description
                                </label>
                                <Textarea
                                    value={drill.description}
                                    onChange={(e) =>
                                        handleDrillChange(index, 'description', e.target.value)
                                    }
                                    className="mt-1"
                                />
                            </div>
                            <div>
                                <label className="block text-sm font-medium">
                                    Related Technique
                                </label>
                                <Select
                                    value={drill.relatedTechniqueName === '' ? '__none__' : drill.relatedTechniqueName}
                                    onValueChange={(value) =>
                                        handleDrillChange(index, 'relatedTechniqueName', value === '__none__' ? '' : value)
                                    }
                                >
                                    <SelectTrigger className="mt-1">
                                        <SelectValue placeholder="Select related technique (optional)" />
                                    </SelectTrigger>
                                    <SelectContent>
                                        <SelectItem value="__none__">None</SelectItem>
                                        {analysisResultDto.techniques?.map((tech) => (
                                            <SelectItem key={tech.name} value={tech.name}>
                                                {tech.name}
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                            </div>
                            <Button
                                onClick={() => deleteDrill(index)}
                                className="bg-red-500 text-white hover:bg-red-600"
                            >
                                Delete Drill
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