// Register.tsx
import {useState} from "react";
import {useNavigate} from "react-router-dom";
import useAuthStore, { RegisterFighterBody } from "@/store/authStore.ts";
import {Button} from "@/components/ui/button.tsx";
import ConfirmationDialog from "@/components/ConfirmationDialog";

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
    // Assuming the existence of a 'register' method in useAuthStore similar to 'login'
    const register = useAuthStore((state) => state.register);
    console.log('testing...', typeof register);
    const registerFighter = useAuthStore((state) => state.registerFighter);

    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [validationError, setValidationError] = useState<ValidationError | null>();

    const [role, setRole] = useState("Student");
    const [name, setName] = useState("");
    const [height, setHeight] = useState('0');
    const [weight, setWeight] = useState('0');
    const [bmi, setBmi] = useState('0');
    const [gender, setGender] = useState("Male");
    const [birthdate, setBirthdate] = useState<string>(''); // This is a string because it is a date input
    const [sparringDuration, setSparringDuration] = useState('5');
    const [experience, setExperience] = useState(TrainingExperience.LessThanTwoYears);
    const [beltRank, setBeltRank] = useState("White");

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
                    navigate("/login"); // Redirect to login page or another page as desired    
                } else {
                    const errorResponse = JSON.parse(success.response ?? "{}");
                    if (errorResponse && typeof errorResponse === 'object') {
                        setValidationError(errorResponse as ValidationError);
                        console.error("Registration failed: ", errorResponse as ValidationError);
                    }
                }
            } catch (error) {
                console.error("Registration failed: ", error);
                setValidationError({type: 'error', title: error as string, status: 500, errors: {}});
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
        const heightInMeters = parseFloat(height) * 0.3048; // Convert height from feet to meters
        const weightInKg = parseFloat(weight) * 0.453592; // Convert weight from pounds to kilograms
        if (heightInMeters > 0 && weightInKg > 0) {
            const bmiValue = weightInKg / (heightInMeters * heightInMeters); // Calculate BMI
            setBmi(bmiValue.toFixed(2)); // Format BMI to 2 decimal places and set it
        }
    };

    return (
        <div className="container mx-auto max-w-md p-8 shadow-lg rounded-lg">
            <h1 className="text-3xl font-bold text-primary mb-6">Register</h1>
            <form
                className="space-y-4"
                onSubmit={handleRegister}
            >
                {validationError &&
                    <>
                        {
                            Object.values(validationError.errors).flat().map((error, index) =>  
                                {
                                    const keyIndex = index;
                                    return <p key={keyIndex} className="text-red-500 line-clamp-5">{error}</p>
                                }
                            )
                        }
                        <p className="text-red-300 text-xl font-bold">Check console for details</p>
                    </>
                }

                { /* FighterRole field */ }
                <div>
                    <label htmlFor="role" className="block text-sm font-medium">Select Role:</label>
                    <select
                        id="role"
                        className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
                        value={role}
                        onChange={(e) => setRole(e.target.value)}
                    >
                        <option value="Student">Student</option>
                        <option value="Instructor">Instructor</option>
                    </select>
                </div>

                { /* Fighter Name field */ }
                <div>
                    <label htmlFor="name" className="block text-sm font-medium">Name:</label>
                    <input
                        type="text"
                        id="name"
                        className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
                        value={name}
                        onChange={(e) => setName(e.target.value)}
                        required
                    />
                </div>

                { /* Height, Weight, BMI fields */ }
                <div className="grid grid-cols-2 gap-4">
                    { /* Height field */ }
                    <div>
                        <label htmlFor="height" className="block text-sm font-medium">Height:</label>
                        <input
                            type="text"
                            id="height"
                            className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
                            value={height}
                            onChange={(e) => setHeight(e.target.value)}
                            onBlur={calculateBmi}
                            required
                        />
                    </div>
                    <div className="flex items-center">
                        <span className="ml-2">in ft</span>
                    </div>

                    { /* Weight field */ }
                    <div>
                        <label htmlFor="weight" className="block text-sm font-medium">Weight:</label>
                        <input
                            type="text"
                            id="weight"
                            className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
                            value={weight}
                            onChange={(e) => setWeight(e.target.value)}
                            onBlur={calculateBmi}
                            required/>
                    </div>
                    <div className="flex items-center">
                        <span className="ml-2">in lbs</span>
                    </div>

                    { /* BMI field */ }
                    <div>
                        <label htmlFor="bmi" className="block text-sm font-medium">BMI:</label>
                        <input
                            type="text"
                            id="bmi"
                            className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
                            value={bmi}
                            readOnly
                        />
                    </div>
                </div>

                {/* Gender field */}
                <div>
                    <label htmlFor="gender" className="block text-sm font-medium">Gender:</label>
                    <select
                        id="gender"
                        className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
                        value={gender}
                        onChange={(e) => setGender(e.target.value)}
                    >
                        <option value="Male">Male</option>
                        <option value="Female">Female</option>
                    </select>
                </div>

                {/* Birthdate field */}
                <div>
                    <label htmlFor="birthdate">Birthdate:</label>
                    <input
                        type="date"
                        value={birthdate}
                        onChange={(e) => setBirthdate(e.target.value)}
                        className="mt-1 block w-full py-2 px-3 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary focus:border-primary sm:text-sm"
                    />
                </div>

                {/* Sparring Duration field */}
                <div>
                    <label htmlFor="sparringDuration" className="block text-sm font-medium">How long can you spar without breaking? (minutes):</label>
                    <input
                        type="text"
                        id="sparringDuration"
                        className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
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
                            <input
                                type="radio"
                                name="experience"
                                value={TrainingExperience.LessThanTwoYears}
                                checked={experience === TrainingExperience.LessThanTwoYears}
                                onChange={(e) => setExperience(Number(e.target.value))}
                            />
                            <span className="ml-2">Less than 2 years</span>
                        </label>
                        <label>
                            <input
                                type="radio"
                                name="experience"
                                value={TrainingExperience.FromTwoToFiveYears}
                                checked={experience === TrainingExperience.FromTwoToFiveYears}
                                onChange={(e) => setExperience(Number(e.target.value))}
                            />
                            <span className="ml-2">from 2 to 5 years</span>
                        </label>
                        <label>
                            <input
                                type="radio"
                                name="experience"
                                value={TrainingExperience.MoreThanFiveYears}
                                checked={experience === TrainingExperience.MoreThanFiveYears}
                                onChange={(e) => setExperience(Number(e.target.value))}
                            />
                            <span className="ml-2">More than 5 years</span>
                        </label>
                    </div>
                </div>

                {/* Belt Rank field */}
                <div>
                    <label htmlFor="beltRank" className="block text-sm font-medium">Brazilian Jiu-Jitsu Belt Rank:</label>
                    <select
                        id="beltRank"
                        className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
                        value={beltRank}
                        onChange={(e) => setBeltRank(e.target.value)}
                    >
                        <option value="White">White</option>
                        <option value="Blue">Blue</option>
                        <option value="Purple">Purple</option>
                        <option value="Brown">Brown</option>
                        <option value="Black">Black</option>
                    </select>
                </div>

                {/* Email field */}
                <div>
                    <label htmlFor="email" className="block text-sm font-medium">
                        Email
                    </label>
                    <input
                        type="email"
                        id="email"
                        name="email"
                        className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
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
                    <label htmlFor="password" className="block text-sm font-medium">
                        Password
                    </label>
                    <input
                        type="password"
                        id="password"
                        name="password"
                        className="mt-1 block w-full px-3 py-2 bg-input border rounded-md shadow-sm"
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
