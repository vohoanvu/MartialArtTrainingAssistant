import React, { useState, useEffect } from 'react';
import { AnalysisResultDto, Strength, AreaForImprovement } from '@/types/global';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { ChevronDown, ChevronUp, PlusIcon, TrashIcon } from 'lucide-react';

interface OverallAnalysisEditorialProps {
    analysisResultDto: AnalysisResultDto;
    handleSaveChanges: (updatedFeedbackData: AnalysisResultDto) => Promise<void>;
    isAnalysisSaving?: boolean;
}

export const OverallAnalysisEditorial: React.FC<OverallAnalysisEditorialProps> = ({
    analysisResultDto,
    handleSaveChanges,
    isAnalysisSaving,
}) => {
    const [overallDescription, setOverallDescription] = useState<string>(
        analysisResultDto.overallDescription ?? ''
    );
    const [strengths, setStrengths] = useState<Strength[]>(
        analysisResultDto.strengths ?? []
    );
    const [areasForImprovement, setAreasForImprovement] = useState<AreaForImprovement[]>(analysisResultDto.areasForImprovement ?? []);
    const [showStrengths, setShowStrengths] = useState(true);
    const [showAreas, setShowAreas] = useState(true);

    useEffect(() => {
        setOverallDescription(analysisResultDto.overallDescription ?? '');
        setStrengths(analysisResultDto.strengths ?? []);
        setAreasForImprovement(analysisResultDto.areasForImprovement ?? []);
    }, [analysisResultDto]);

    const handleStrengthChange = <T extends keyof Strength>(
        index: number,
        field: T,
        value: Strength[T]
    ) => {
        const updatedStrengths = [...strengths];
        updatedStrengths[index][field] = value;
        setStrengths(updatedStrengths);
    };

    const handleAreaChange = <T extends keyof AreaForImprovement>(
        index: number,
        field: T,
        value: AreaForImprovement[T]
    ) => {
        const updatedAreas = [...areasForImprovement];
        updatedAreas[index][field] = value;
        setAreasForImprovement(updatedAreas);
    };

    const addStrength = () => {
        setStrengths([...strengths, { description: '', relatedTechniqueId: 0 }]);
    };

    const addArea = () => {
        setAreasForImprovement([
            ...areasForImprovement,
            { description: '', weaknessCategory: '', relatedTechniqueId: 0, keywords: '' },
        ]);
    };

    const deleteStrength = (index: number) => {
        setStrengths(strengths.filter((_, i) => i !== index));
    };

    const deleteArea = (index: number) => {
        setAreasForImprovement(areasForImprovement.filter((_, i) => i !== index));
    };

    const saveChanges = async () => {
        const updatedFeedbackData: AnalysisResultDto = {
            overallDescription,
            strengths,
            areasForImprovement,
        };
        await handleSaveChanges(updatedFeedbackData);
    };

    return (
        <div className="space-y-4">
            <div className="p-4 bg-background rounded-md shadow border border-border">
                <label className="block text-sm font-medium text-foreground">
                    Overall Description
                </label>
                <Textarea
                    value={overallDescription}
                    onChange={(e) => setOverallDescription(e.target.value)}
                    onBlur={(e) => setOverallDescription(e.target.value)}
                    className="mt-1 h-24"
                    placeholder="Enter overall analysis description"
                />
            </div>

            <div className="p-4 bg-background rounded-md shadow border border-border">
                <div
                    className="flex justify-between items-center cursor-pointer p-2 hover:bg-accent/40"
                    onClick={() => setShowStrengths(!showStrengths)}
                >
                    <h3 className="text-lg font-bold text-foreground">Strengths</h3>
                    {showStrengths ? (
                        <ChevronUp className="text-primary" />
                    ) : (
                        <ChevronDown className="text-primary" />
                    )}
                </div>
                {showStrengths && (
                    <div className="mt-2 space-y-2">
                        {
                            strengths.map((strength, index) => {
                                const relatedTechnique =analysisResultDto.techniques?.find(t => strength.relatedTechniqueId && t.id === strength.relatedTechniqueId);
                                const selectValue = relatedTechnique?.name || '__none__';

                                return (
                                    <div key={index} className="p-4 bg-background rounded-md border border-border">
                                        <label className="block text-sm font-medium text-foreground">
                                            Strength {index + 1} Description
                                        </label>
                                        <Textarea
                                            value={strength.description}
                                            onChange={(e) =>
                                                handleStrengthChange(index, 'description', e.target.value)
                                            }
                                            className="mt-1"
                                            placeholder="Enter strength description"
                                        />
                                        <label className="block text-sm font-medium mt-2 text-foreground">
                                            Related Technique
                                        </label>
                                        <Select
                                            value={!strength.relatedTechniqueId ? '__none__' : selectValue}
                                            onValueChange={(value) => {
                                                const selected = analysisResultDto.techniques?.find(t => t.name === value);
                                                handleStrengthChange(index, 'relatedTechniqueId', value === '__none__' ? 0 : (selected?.id ?? 0));
                                            }}
                                        >
                                            <SelectTrigger className="mt-1">
                                                <SelectValue placeholder="None" />
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
                                        <Button
                                            onClick={() => deleteStrength(index)}
                                            className="mt-2"
                                            variant="destructive"
                                        >
                                            <TrashIcon/>
                                        </Button>
                                    </div>
                                );
                            })
                        }
                        <Button
                            onClick={addStrength}
                            className="mt-2"
                            variant="outline"
                        >
                            <PlusIcon/>
                        </Button>
                    </div>
                )}
            </div>

            <div className="p-4 bg-background rounded-md shadow border border-border">
                <div
                    className="flex justify-between items-center cursor-pointer p-2 hover:bg-accent/40"
                    onClick={() => setShowAreas(!showAreas)}
                >
                    <h3 className="text-lg font-bold text-foreground">Areas for Improvement</h3>
                    {showAreas ? (
                        <ChevronUp className="text-primary" />
                    ) : (
                        <ChevronDown className="text-primary" />
                    )}
                </div>
                {showAreas && (
                    <div className="mt-2 space-y-2">
                        {
                            areasForImprovement.map((area, index) => {
                                const relatedTechnique =analysisResultDto.techniques?.find(t => area.relatedTechniqueId && t.id === area.relatedTechniqueId);
                                const selectImprovementTechniqueValue = relatedTechnique?.name || '__none__';

                                return (
                                    <div key={index} className="p-4 bg-background rounded-md border border-border">
                                        <label className="block text-sm font-medium text-foreground">
                                            Area for Improvement {index + 1} Description
                                        </label>
                                        <Textarea
                                            value={area.description}
                                            onChange={(e) =>
                                                handleAreaChange(index, 'description', e.target.value)
                                            }
                                            className="mt-1"
                                            placeholder="Enter area for improvement description"
                                        />
                                        <label className="block text-sm font-medium mt-2 text-foreground">
                                            Related Technique
                                        </label>
                                        <Select
                                            value={!area.relatedTechniqueId ? '__none__' : selectImprovementTechniqueValue}
                                            onValueChange={(value) => {
                                                const selected = analysisResultDto.techniques?.find(t => t.name === value);
                                                handleAreaChange(index, 'relatedTechniqueId', value === '__none__' ? 0 : (selected?.id ?? 0));
                                            }}
                                        >
                                            <SelectTrigger className="mt-1">
                                                <SelectValue placeholder="None" />
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
                                        <Button
                                            onClick={() => deleteArea(index)}
                                            className="mt-2"
                                            variant="destructive"
                                        >
                                            <TrashIcon/>
                                        </Button>
                                    </div>
                                );
                            })
                        }
                        <Button
                            onClick={addArea}
                            className="mt-2"
                            variant="outline"
                        >
                            <PlusIcon/>
                        </Button>
                    </div>
                )}
            </div>

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