import React, { useState, useEffect } from 'react';
import { AnalysisResultDto, SuggestedDrill } from '@/types/global';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { ChevronDown, ChevronUp, Pencil, TrashIcon, PlusIcon } from 'lucide-react';

interface DrillsEditorialProps {
    analysisResultDto: AnalysisResultDto;
    handleSaveChanges: (updatedFeedbackData: AnalysisResultDto) => Promise<void>;
    isAnalysisSaving?: boolean;
}

export const DrillsEditorial: React.FC<DrillsEditorialProps> = ({
    analysisResultDto,
    handleSaveChanges,
    isAnalysisSaving,
}) => {
    const [drills, setDrills] = useState<SuggestedDrill[]>(analysisResultDto.drills ?? []);
    const [showCreateForm, setShowCreateForm] = useState(false);
    const [newDrill, setNewDrill] = useState<SuggestedDrill>({
        name: '',
        focus: '',
        duration: '',
        description: '',
        relatedTechniqueName: '',
        relatedTechniqueId: 0,
    });
    const [expandedDrills, setExpandedDrills] = useState<boolean[]>([]);
    const [editingNameIndex, setEditingNameIndex] = useState<number | null>(null);
    const [editingNameValue, setEditingNameValue] = useState<string>('');

    useEffect(() => {
        // Remove duplicate "Generic" drills, keep only the first occurrence
        const drills = (analysisResultDto.drills ?? []).filter(
            (drill, idx, arr) =>
                drill.relatedTechniqueName !== "Generic" ||
                arr.findIndex(d => d.relatedTechniqueName === "Generic") === idx
        );
        setDrills(drills);
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
        value: string | number
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
        value: string | number
    ) => {
        setNewDrill((prev) => ({ ...prev, [field]: value }));
    };

    const addDrill = () => {
        setDrills([...drills, { ...newDrill }]);
        setExpandedDrills([...expandedDrills, false]);
        setNewDrill({
            name: '',
            focus: '',
            duration: '',
            description: '',
            relatedTechniqueName: '',
            relatedTechniqueId: 0,
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

    const startEditingName = (index: number, currentName: string) => {
        setEditingNameIndex(index);
        setEditingNameValue(currentName);
    };

    const saveEditingName = (index: number) => {
        handleDrillChange(index, 'name', editingNameValue);
        setEditingNameIndex(null);
        setEditingNameValue('');
    };

    // Find the related technique for the new drill (if any)
    const newDrillRelatedTechnique = analysisResultDto.techniques?.find(t =>
        (newDrill.relatedTechniqueId && t.id === newDrill.relatedTechniqueId) ||
        (newDrill.relatedTechniqueName && t.name === newDrill.relatedTechniqueName)
    );
    const newDrillSelectValue = newDrillRelatedTechnique?.name || '__none__';

    return (
        <div className="space-y-4">
            <Button
                onClick={() => setShowCreateForm(!showCreateForm)}
                className=""
                variant="default"
            >
                <PlusIcon/>
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
                            value={newDrillSelectValue}
                            onValueChange={(value) => {
                                if (value === '__none__') {
                                    handleNewDrillChange('relatedTechniqueName', '');
                                    handleNewDrillChange('relatedTechniqueId', 0);
                                    alert("Please select a related Technique for this Drill");
                                } else {
                                    handleNewDrillChange('relatedTechniqueName', value);
                                    const selected = analysisResultDto.techniques?.find(t => t.name === value);
                                    handleNewDrillChange('relatedTechniqueId', selected?.id ?? 0);
                                }
                            }}
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

            {drills.map((drill, index) => {
                const relatedTechnique =
                    analysisResultDto.techniques?.find(
                        t =>
                            (drill.relatedTechniqueId && t.id === drill.relatedTechniqueId) ||
                            (drill.relatedTechniqueName && t.name === drill.relatedTechniqueName)
                    );
                const selectValue = relatedTechnique?.name || '__none__';

                return (
                    <div key={index} className="border-b border-border pb-4">
                        <div className="flex justify-between items-center cursor-pointer p-2 hover:bg-accent/40" onClick={() => toggleDrillDetails(index)}>
                            <p className="flex items-center gap-2">
                                <strong>Drill Name:</strong>
                                {editingNameIndex === index ? (
                                    <>
                                        <Input
                                            value={editingNameValue}
                                            onChange={e => setEditingNameValue(e.target.value)}
                                            className="w-auto max-w-xs h-7 px-2 py-1 text-sm"
                                            onClick={e => e.stopPropagation()}
                                            onKeyDown={e => {
                                                if (e.key === 'Enter') saveEditingName(index);
                                                if (e.key === 'Escape') setEditingNameIndex(null);
                                            }}
                                            autoFocus
                                        />
                                        <Button
                                            size="icon"
                                            variant="ghost"
                                            className="h-7 w-7"
                                            onClick={e => {
                                                e.stopPropagation();
                                                saveEditingName(index);
                                            }}
                                            title="Save"
                                        >
                                            <span className="sr-only">Save</span>
                                            <Pencil className="w-4 h-4 text-primary" />
                                        </Button>
                                    </>
                                ) : (
                                    <>
                                        <span className="text-foreground">{drill.name}</span>
                                        <Button
                                            size="icon"
                                            variant="ghost"
                                            className="h-7 w-7"
                                            onClick={e => {
                                                e.stopPropagation();
                                                startEditingName(index, drill.name);
                                            }}
                                            title="Edit"
                                        >
                                            <span className="sr-only">Edit</span>
                                            <Pencil className="w-4 h-4 text-primary" />
                                        </Button>
                                    </>
                                )}
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
                                        value={selectValue}
                                        onValueChange={(value) => {
                                            if (value === '__none__') {
                                                handleDrillChange(index, 'relatedTechniqueName', '');
                                                handleDrillChange(index, 'relatedTechniqueId', 0);
                                            } else {
                                                handleDrillChange(index, 'relatedTechniqueName', value);
                                                const selected = analysisResultDto.techniques?.find(t => t.name === value);
                                                handleDrillChange(index, 'relatedTechniqueId', selected?.id ?? 0);
                                            }
                                        }}
                                    >
                                        <SelectTrigger className="mt-1">
                                            <SelectValue placeholder="Select related technique (optional)" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="__none__">None</SelectItem>
                                            {analysisResultDto.techniques?.map((tech) => (
                                                <SelectItem key={tech.id ?? tech.name} value={tech.name}>
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
                                    <TrashIcon/>
                                </Button>
                            </div>
                        )}
                    </div>
                );
            }
            )}

            <Button
                onClick={saveChanges}
                className="mt-4"
                variant="default"
                size='lg'
            >
                {isAnalysisSaving ? "Saving changes..." : "Save Changes"}
            </Button>
        </div>
    );
};