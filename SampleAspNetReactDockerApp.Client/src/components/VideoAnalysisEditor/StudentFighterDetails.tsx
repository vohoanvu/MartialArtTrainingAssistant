import { Fighter } from "@/types/global";
import { useEffect, useState } from "react";
import { Input } from "../ui/input";

interface StudentDetailsProps {
    fighterDetails: Fighter | null;
    studentIdentifier: string | null;
}

enum TrainingExperience {
    LessThanTwoYears = "Less than 2 years",
    FromTwoToFiveYears = "2 to 5 years",
    MoreThanFiveYears = "More than 5 years",
};

function getTrainingExperienceFromIndex(index: number): TrainingExperience | undefined {
    // Get the keys of the enum in their declaration order
    const keys = Object.keys(TrainingExperience) as Array<keyof typeof TrainingExperience>;

    // Check if the index is valid
    if (index >= 0 && index < keys.length) {
        const key = keys[index]; // Get the key name (e.g., "FromTwoToFiveYears")
        return TrainingExperience[key]; // Get the corresponding string value (e.g., "2 to 5 years")
    }

    return undefined; // Return undefined if the index is out of bounds
}

export const StudentDetails: React.FC<StudentDetailsProps> = ({
    fighterDetails,
    studentIdentifier,
}) => {

    return (
        <div id="studentDetails" className="grid grid-cols-2 gap-4 p-4 m-2 shadow-md border">
            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Student Name</label>
                <Input
                    type="text"
                    value={fighterDetails?.fighterName ?? ''}
                    placeholder="Enter student name..."
                    className="w-full p-2 border rounded-md"
                    readOnly
                />
            </div>

            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Belt Rank</label>
                <Input
                    type="text"
                    value={fighterDetails?.beltColor ?? ''}
                    className="w-full p-2 border rounded-md"
                    placeholder="Enter belt rank..."
                    readOnly
                />
            </div>

            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Student Identifier</label>
                <Input
                    type="text"
                    value={studentIdentifier ?? ''}
                    className="w-full p-2 border rounded-md"
                    placeholder="Enter student identifier..."
                    readOnly
                />
            </div>

            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Height</label>
                <Input
                    type="text"
                    value={fighterDetails?.height ?? ''}
                    className="w-full p-2 border rounded-md"
                    placeholder="Enter height..."
                    readOnly
                />
            </div>

            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Weight</label>
                <Input
                    type="text"
                    value={fighterDetails?.weight ?? ''}
                    className="w-full p-2 border rounded-md"
                    placeholder="Enter weight..."
                    readOnly
                />
            </div>

            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Training Experience</label>
                <Input
                    type="text"
                    value={getTrainingExperienceFromIndex(fighterDetails?.experience as number) || TrainingExperience.LessThanTwoYears}
                    className="w-full p-2 border rounded-md"
                    placeholder="Enter training experience..."
                    readOnly
                />
            </div>
        </div>
    );
};