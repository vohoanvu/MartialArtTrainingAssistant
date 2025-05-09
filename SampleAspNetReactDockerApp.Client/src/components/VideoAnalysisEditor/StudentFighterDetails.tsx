import { Fighter } from "@/types/global";
import { useEffect, useState } from "react";

interface StudentDetailsProps {
    fighterDetails: Fighter | null;
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
}) => {
    const [studentName, setStudentName] = useState('');
    const [beltRantk, setBeltRank] = useState('');
    const [studentIdentifier, setStudentIdentifier] = useState('');
    const [experience, setExperience] = useState(TrainingExperience.MoreThanFiveYears);
    const [height, setHeight] = useState('0');
    const [weight, setWeight] = useState('0');

    useEffect(() => {
        if (fighterDetails) {
            setStudentName(fighterDetails.fighterName);
            setBeltRank(fighterDetails.beltColor);
            setStudentIdentifier("TO BE CONTINUED");
            setHeight(fighterDetails.height.toString());
            setWeight(fighterDetails.weight.toString());
            setExperience(getTrainingExperienceFromIndex(fighterDetails.experience as number) || TrainingExperience.LessThanTwoYears);
        }
    }, [fighterDetails]);

    return (
        <div id="studentDetails" className="grid grid-cols-2 gap-4 p-4 m-2 shadow-md border">
            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Student Name</label>
                <input
                    type="text"
                    value={studentName}
                    onChange={(e) => setStudentName(e.target.value)}
                    className="w-full p-2 border rounded-md"
                    placeholder="Enter student name..."
                    readOnly
                />
            </div>

            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Belt Rank</label>
                <input
                    type="text"
                    value={beltRantk}
                    onChange={(e) => setBeltRank(e.target.value)}
                    className="w-full p-2 border rounded-md"
                    placeholder="Enter belt rank..."
                    readOnly
                />
            </div>

            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Student Identifier</label>
                <input
                    type="text"
                    value={studentIdentifier}
                    onChange={(e) => setStudentIdentifier(e.target.value)}
                    className="w-full p-2 border rounded-md"
                    placeholder="Enter student identifier..."
                    readOnly
                />
            </div>

            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Height</label>
                <input
                    type="text"
                    value={height}
                    className="w-full p-2 border rounded-md"
                    placeholder="Enter height..."
                    readOnly
                />
            </div>

            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Weight</label>
                <input
                    type="text"
                    value={weight}
                    className="w-full p-2 border rounded-md"
                    placeholder="Enter weight..."
                    readOnly
                />
            </div>

            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Training Experience</label>
                <input
                    type="text"
                    value={experience}
                    className="w-full p-2 border rounded-md"
                    placeholder="Enter training experience..."
                    readOnly
                />
            </div>
        </div>
    );
};