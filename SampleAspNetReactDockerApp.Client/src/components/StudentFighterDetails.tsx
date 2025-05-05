import { Fighter } from "@/types/global";
import { useEffect, useState } from "react";

interface StudentDetailsProps {
    fighterDetails: Fighter | null;
}

export const StudentDetails: React.FC<StudentDetailsProps> = ({
    fighterDetails,
}) => {
    const [studentName, setStudentName] = useState('');
    const [beltRantk, setBeltRank] = useState('');
    const [studentIdentifier, setStudentIdentifier] = useState('');
    const [experience, setExperience] = useState('0');
    const [height, setHeight] = useState('0');
    const [weight, setWeight] = useState('0');

    useEffect(() => {
        if (fighterDetails) {
            setStudentName(fighterDetails.fighterName);
            setBeltRank(fighterDetails.beltColor);
            setStudentIdentifier("TO BE CONTINUED");
        }
    }, [fighterDetails]);

    return (
        <div id="studentDetails" className="grid grid-cols-2 gap-4">
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
                    onChange={(e) => setHeight(e.target.value)}
                    className="w-full p-2 border rounded-md"
                    placeholder="Enter height..."
                />
            </div>

            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Weight</label>
                <input
                    type="text"
                    value={weight}
                    onChange={(e) => setWeight(e.target.value)}
                    className="w-full p-2 border rounded-md"
                    placeholder="Enter weight..."
                />
            </div>

            <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Training Experience</label>
                <input
                    type="text"
                    value={experience}
                    onChange={(e) => setExperience(e.target.value)}
                    className="w-full p-2 border rounded-md"
                    placeholder="Enter training experience..."
                />
            </div>
        </div>
    );
};