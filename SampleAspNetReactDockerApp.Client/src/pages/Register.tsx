import { useState } from "react";
import { useNavigate } from "react-router-dom";
import useAuthStore, { RegisterFighterBody } from "@/store/authStore.ts";
import { Button } from "@/components/ui/button.tsx";
import { Input } from "@/components/ui/input";
import { Select, SelectTrigger, SelectContent, SelectItem, SelectValue } from "@/components/ui/select";
import ConfirmationDialog from "@/components/ui/ConfirmationDialog";

interface ValidationError {
    type: string;
    title: string;
    status: number;
    errors: {
        PasswordTooShort?: string[];
        PasswordRequiresNonAlphanumeric?: string[];
        PasswordRequiresLower?: string[];
        PasswordRequiresUpper?: string[];
    };
}

enum TrainingExperience {
    LessThanTwoYears = 0,
    FromTwoToFiveYears = 1,
    MoreThanFiveYears = 2
}

export default function Register() {
    const navigate = useNavigate();
    const registerFighter = useAuthStore((state) => state.registerFighter);

    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [validationError, setValidationError] = useState<ValidationError | null>();

    const [role, setRole] = useState("Instructor");
    const [name, setName] = useState("");
    const [height, setHeight] = useState('0');
    const [weight, setWeight] = useState('0');
    const [bmi, setBmi] = useState('0');
    const [gender, setGender] = useState("Male");
    const [birthdate, setBirthdate] = useState<string>('');
    const [sparringDuration, setSparringDuration] = useState('5');
    const [experience, setExperience] = useState(TrainingExperience.LessThanTwoYears);
    const [beltRank, setBeltRank] = useState("White");

    const [ftImperial, setFtImperial] = useState('');
    const [lbsImperial, setLbsImperial] = useState('');

    const [isDialogOpen, setIsDialogOpen] = useState(false);
    const [pendingSubmit, setPendingSubmit] = useState<React.FormEvent<HTMLFormElement> | null>(null);

    const handleRegister = async (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setPendingSubmit(event);
        setIsDialogOpen(true);
    };

    const handleConfirm = async () => {
        if (pendingSubmit) {
            const fighterRegistrationPayload: RegisterFighterBody = {
                email: email,
                password: password,
                fighterRole: role,
                fighterName: name,
                height: parseFloat(height),
                weight: parseFloat(weight),
                bmi: parseFloat(bmi),
                gender: gender,
                birthdate: birthdate,
                maxWorkoutDuration: parseFloat(sparringDuration),
                experience: experience,
                beltColor: beltRank
            };
            try {
                const success = await registerFighter(fighterRegistrationPayload);
                if (success.successful) {
                    alert("Registration successful!");
                    navigate("/home");
                } else {
                    const errorResponse = JSON.parse(success.response ?? "{}");
                    if (errorResponse && typeof errorResponse === 'object') {
                        setValidationError(errorResponse as ValidationError);
                        console.error("Registration failed: ", errorResponse as ValidationError);
                    }
                }
            } catch (error) {
                console.error("Registration failed: ", error);
                setValidationError({ type: 'error', title: error as string, status: 500, errors: {} });
            }

            setIsDialogOpen(false);
            setPendingSubmit(null);
        }
    };

    const handleCancel = () => {
        setIsDialogOpen(false);
        setPendingSubmit(null);
    };

    const calculateBmi = () => {
        const heightInMeters = parseFloat(height) / 100;
        const weightInKg = parseFloat(weight);
        if (heightInMeters > 0 && weightInKg > 0) {
            const bmiValue = weightInKg / (heightInMeters * heightInMeters);
            setBmi(bmiValue.toFixed(2));
        }
    };

    const applyImperialConversion = () => {
        const convertedHeight = ftImperial ? (parseFloat(ftImperial) * 30.48).toFixed(1) : height;
        const convertedWeight = lbsImperial ? (parseFloat(lbsImperial) * 0.45359237).toFixed(1) : weight;
        setHeight(convertedHeight);
        setWeight(convertedWeight);
        setFtImperial('');
        setLbsImperial('');
        const heightInMeters = parseFloat(convertedHeight) / 100;
        const weightInKg = parseFloat(convertedWeight);
        if (heightInMeters > 0 && weightInKg > 0) {
            const bmiValue = weightInKg / (heightInMeters * heightInMeters);
            setBmi(bmiValue.toFixed(2));
        }
    };

    return (
        <div className="container mx-auto max-w-4xl p-8 shadow-lg rounded-lg">
            <h1 className="text-3xl font-bold text-primary mb-6">Register</h1>
            <form className="space-y-4" onSubmit={handleRegister}>
                {validationError &&
                    <>
                        {Object.values(validationError.errors).flat().map((error, index) =>
                            <p key={index} className="text-red-500 line-clamp-5">{error}</p>
                        )}
                        <p className="text-red-300 text-xl font-bold">Check console for details</p>
                    </>
                }

                {/* FighterRole field */}
                <div>
                    <label htmlFor="role" className="block text-sm font-medium">Select Role:</label>
                    <Select value={role} onValueChange={setRole}>
                        <SelectTrigger id="role">
                            <SelectValue placeholder="Select role" />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value="Student">Student</SelectItem>
                            <SelectItem value="Instructor">Instructor</SelectItem>
                        </SelectContent>
                    </Select>
                </div>

                {/* Fighter Name field */}
                <div>
                    <label htmlFor="name" className="block text-sm font-medium">Name:</label>
                    <Input
                        type="text"
                        id="name"
                        value={name}
                        onChange={(e) => setName(e.target.value)}
                        required
                    />
                </div>

                {/* Height, Weight, BMI fields */}
                <div className="grid grid-cols-2 gap-4">
                    <div>
                        <label htmlFor="height" className="block text-sm font-medium">Height:</label>
                        <Input
                            type="text"
                            id="height"
                            value={height}
                            onChange={(e) => setHeight(e.target.value)}
                            onBlur={calculateBmi}
                            required
                        />
                    </div>
                    <div className="flex items-center">
                        <span className="ml-2">in CM</span>
                    </div>
                    <div>
                        <label htmlFor="weight" className="block text-sm font-medium">Weight:</label>
                        <Input
                            type="text"
                            id="weight"
                            value={weight}
                            onChange={(e) => setWeight(e.target.value)}
                            onBlur={calculateBmi}
                            required
                        />
                    </div>
                    <div className="flex items-center">
                        <span className="ml-2">in KG</span>
                    </div>
                    <div>
                        <label htmlFor="bmi" className="block text-sm font-medium">BMI:</label>
                        <Input
                            type="text"
                            id="bmi"
                            value={bmi}
                            readOnly
                        />
                    </div>
                </div>

                {/* Imperial Conversion Tool */}
                <div className="border border-dashed border-gray-300 rounded-lg p-4 mt-4">
                    <h3 className="text-lg font-semibold mb-2">Imperial Conversion Tool</h3>
                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label htmlFor="imperialHeight" className="block text-sm font-medium">Height (FT):</label>
                            <Input
                                type="text"
                                id="imperialHeight"
                                value={ftImperial}
                                onChange={(e) => setFtImperial(e.target.value)}
                                placeholder="e.g., 5.8"
                            />
                        </div>
                        <div className="flex items-center">
                            <span className="ml-2">Converted to CM: {ftImperial ? (parseFloat(ftImperial) * 30.48).toFixed(1) : "0"}</span>
                        </div>
                        <div>
                            <label htmlFor="imperialWeight" className="block text-sm font-medium">Weight (LBS):</label>
                            <Input
                                type="text"
                                id="imperialWeight"
                                value={lbsImperial}
                                onChange={(e) => setLbsImperial(e.target.value)}
                                placeholder="e.g., 150"
                            />
                        </div>
                        <div className="flex items-center">
                            <span className="ml-2">Converted to KG: {lbsImperial ? (parseFloat(lbsImperial) * 0.45359237).toFixed(1) : "0"}</span>
                        </div>
                    </div>
                    <div className="mt-2">
                        <Button variant="secondary" onClick={applyImperialConversion} className="text-sm">
                            Apply Conversion
                        </Button>
                    </div>
                </div>

                {/* Gender field */}
                <div>
                    <label htmlFor="gender" className="block text-sm font-medium">Gender:</label>
                    <Select value={gender} onValueChange={setGender}>
                        <SelectTrigger id="gender">
                            <SelectValue placeholder="Select gender" />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value="Male">Male</SelectItem>
                            <SelectItem value="Female">Female</SelectItem>
                        </SelectContent>
                    </Select>
                </div>

                {/* Birthdate field */}
                <div>
                    <label htmlFor="birthdate" className="block text-sm font-medium">Birthdate:</label>
                    <Input
                        type="date"
                        id="birthdate"
                        value={birthdate}
                        onChange={(e) => setBirthdate(e.target.value)}
                    />
                </div>

                {/* Sparring Duration field */}
                <div>
                    <label htmlFor="sparringDuration" className="block text-sm font-medium">How long can you spar without breaking? (minutes):</label>
                    <Input
                        type="text"
                        id="sparringDuration"
                        value={sparringDuration}
                        onChange={(e) => setSparringDuration(e.target.value)}
                        required
                    />
                </div>

                {/* Experience field */}
                <div>
                    <label htmlFor="experience" className="block text-sm font-medium">Years of Training Experience:</label>
                    <div className="flex space-x-4">
                        <label>
                            <Input
                                type="radio"
                                name="experience"
                                value={TrainingExperience.LessThanTwoYears}
                                checked={experience === TrainingExperience.LessThanTwoYears}
                                onChange={(e) => setExperience(Number(e.target.value))}
                                className="w-4 h-4"
                            />
                            <span className="ml-2">Less than 2 years</span>
                        </label>
                        <label>
                            <Input
                                type="radio"
                                name="experience"
                                value={TrainingExperience.FromTwoToFiveYears}
                                checked={experience === TrainingExperience.FromTwoToFiveYears}
                                onChange={(e) => setExperience(Number(e.target.value))}
                                className="w-4 h-4"
                            />
                            <span className="ml-2">from 2 to 5 years</span>
                        </label>
                        <label>
                            <Input
                                type="radio"
                                name="experience"
                                value={TrainingExperience.MoreThanFiveYears}
                                checked={experience === TrainingExperience.MoreThanFiveYears}
                                onChange={(e) => setExperience(Number(e.target.value))}
                                className="w-4 h-4"
                            />
                            <span className="ml-2">More than 5 years</span>
                        </label>
                    </div>
                </div>

                {/* Belt Rank field */}
                <div>
                    <label htmlFor="beltRank" className="block text-sm font-medium">Brazilian Jiu-Jitsu Belt Rank:</label>
                    <Select value={beltRank} onValueChange={setBeltRank}>
                        <SelectTrigger id="beltRank">
                            <SelectValue placeholder="Select belt rank" />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value="White">White</SelectItem>
                            <SelectItem value="Blue">Blue</SelectItem>
                            <SelectItem value="Purple">Purple</SelectItem>
                            <SelectItem value="Brown">Brown</SelectItem>
                            <SelectItem value="Black">Black</SelectItem>
                        </SelectContent>
                    </Select>
                </div>

                {/* Email field */}
                <div>
                    <label htmlFor="email" className="block text-sm font-medium">Email</label>
                    <Input
                        type="email"
                        id="email"
                        name="email"
                        value={email}
                        onChange={(e) => {
                            if (validationError !== null)
                                setValidationError(null);
                            setEmail(e.target.value);
                        }}
                        required
                    />
                </div>
                {/* Password field */}
                <div>
                    <label htmlFor="password" className="block text-sm font-medium">Password</label>
                    <Input
                        type="password"
                        id="password"
                        name="password"
                        value={password}
                        onChange={(e) => {
                            if (validationError !== null)
                                setValidationError(null);
                            setPassword(e.target.value)
                        }}
                        required
                    />
                </div>

                <Button type="submit" variant="outline" className="w-full">
                    Register
                </Button>
                <ConfirmationDialog
                    title="Are you sure all of your details are correct?"
                    message="*You cannot update your details once submitted!"
                    isOpen={isDialogOpen}
                    onConfirm={handleConfirm}
                    onCancel={handleCancel}
                />
            </form>
        </div>
    );
}