import React, { useState, useEffect, useMemo } from 'react';
import { AnalysisResultDto, TechniqueDto } from '@/types/global';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { ChevronDown, ChevronUp, Pencil } from 'lucide-react';

interface TechniquesEditorialProps {
    analysisResultDto: AnalysisResultDto;
    onSeek: (timestamp: string) => void;
    handleSaveChanges: (updatedFeedbackData: AnalysisResultDto) => Promise<void>;
    selectedSegment?: { start: string; end: string } | null;
}

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
            positionalScenarioId: 0
        },
        positionalScenario: { name: '' },
        startTimestamp: '',
        endTimestamp: '',
    });
    const [expandedTechniques, setExpandedTechniques] = useState<boolean[]>([]);
    const [editingNameIndex, setEditingNameIndex] = useState<number | null>(null);
    const [editingNameValue, setEditingNameValue] = useState<string>('');

    // Get distinct Technique Types and Positional Scenarios from analysisResultDto
    const techniqueTypes = useMemo(() => {
        const allTypes = (analysisResultDto.techniques ?? [])
            .map(t => t.techniqueType?.name)
            .filter(Boolean);
        return Array.from(new Set(allTypes));
    }, [analysisResultDto.techniques]);

    const positionalScenarios = useMemo(() => {
        const allScenarios = (analysisResultDto.techniques ?? [])
            .map(t => t.positionalScenario?.name)
            .filter(Boolean);
        return Array.from(new Set(allScenarios));
    }, [analysisResultDto.techniques]);

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
                positionalScenarioId: updatedTechniques[index].techniqueType.positionalScenarioId || updatedTechniques[index].positionalScenario.id || 0
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
            setNewTechnique((prev) => ({ ...prev, [field]: { id: prev[field].id, name: value } }));
            if (field === 'techniqueType') {
                setNewTechnique((prev) => ({ 
                    ...prev, 
                    [field]: { id: prev[field].id, name: value, positionalScenarioId: prev[field].positionalScenarioId }
                }));
            }
        } else {
            setNewTechnique((prev) => ({ ...prev, [field]: value }));
        }
    };

    const addTechnique = () => {
        setTechniques([...techniques, { ...newTechnique }]);
        setExpandedTechniques([...expandedTechniques, false]);
        setNewTechnique({
            name: '',
            description: '',
            techniqueType: {
                name: '',
                positionalScenarioId: 0
            },
            positionalScenario: { name: '' },
            startTimestamp: '',
            endTimestamp: '',
        });
        setShowCreateForm(false);
    };

    const startEditingName = (index: number, currentName: string) => {
        setEditingNameIndex(index);
        setEditingNameValue(currentName);
    };

    const saveEditingName = (index: number) => {
        handleTechniqueChange(index, 'name', editingNameValue);
        setEditingNameIndex(null);
        setEditingNameValue('');
    };

    const deleteTechnique = (index: number) => {
        setTechniques(techniques.filter((_, i) => i !== index));
        setExpandedTechniques(expandedTechniques.filter((_, i) => i !== index));
    };

    const saveChanges = async () => {
        const updatingPartialFeedbackData: AnalysisResultDto = {
            techniques,
        };
        console.log('Sending to PATCH /api/video/{}/analysis with body.... ', updatingPartialFeedbackData);
        await handleSaveChanges(updatingPartialFeedbackData);
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
                <div className="p-4 bg-background rounded-md space-y-4 border border-border">
                    <p className="text-sm text-muted-foreground">
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
                    className="border-b border-border pb-4"
                >
                    <div
                        className="flex justify-between items-center cursor-pointer p-2 hover:bg-accent/40"
                        onClick={() => toggleTechniqueDetails(index)}
                    >
                        <div>
                            <p className="flex items-center gap-2">
                                <strong>Technique Name:</strong>
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
                                        <span className="text-foreground">{technique.name}</span>
                                        <Button
                                            size="icon"
                                            variant="ghost"
                                            className="h-7 w-7"
                                            onClick={e => {
                                                e.stopPropagation();
                                                startEditingName(index, technique.name);
                                            }}
                                            title="Edit"
                                        >
                                            <span className="sr-only">Edit</span>
                                            <Pencil className="w-4 h-4 text-primary" />
                                        </Button>
                                    </>
                                )}
                            </p>
                            <p>
                                <strong>Timestamp:</strong>{' '}
                                <span className="px-1 rounded bg-muted text-muted-foreground">
                                    {technique.startTimestamp}
                                </span>{' '}
                                -{' '}
                                <span className="px-1 rounded bg-muted text-muted-foreground">
                                    {technique.endTimestamp}
                                </span>
                            </p>
                        </div>
                        {expandedTechniques[index] ? (
                            <ChevronUp className="text-primary" />
                        ) : (
                            <ChevronDown className="text-primary" />
                        )}
                    </div>

                    {expandedTechniques[index] && (
                        <div className="p-2 space-y-2 bg-background rounded-md border border-border">
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
                                    onBlur={(e) => handleTechniqueChange(index, 'description', e.target.value)}
                                />
                            </div>
                            <Button
                                onClick={() => onSeek(technique.startTimestamp ?? 'empty timestamp')}
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