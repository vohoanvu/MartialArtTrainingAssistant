import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import useAuthStore, { RegisterFighterBody } from "@/store/authStore.ts";
import { Button } from "@/components/ui/button.tsx";
import { Input } from "@/components/ui/input";
import { Select, SelectTrigger, SelectContent, SelectItem, SelectValue } from "@/components/ui/select";
import ConfirmationDialog from "@/components/ui/ConfirmationDialog";
import { PageWrapper } from "@/components/zen/PageWrapper";
import { SectionHeader } from "@/components/zen/SectionHeader";

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
    const [searchParams] = useSearchParams();
    const roleParams = searchParams.get("role");
    const navigate = useNavigate();
    const registerFighter = useAuthStore((state) => state.registerFighter);

    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [validationError, setValidationError] = useState<ValidationError | null>();

    const [role, setRole] = useState(roleParams ?? "Instructor");
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
                email, password,
                fighterRole: role, fighterName: name,
                height: parseFloat(height), weight: parseFloat(weight),
                bmi: parseFloat(bmi), gender, birthdate,
                maxWorkoutDuration: parseFloat(sparringDuration),
                experience, beltColor: beltRank
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
            setBmi((weightInKg / (heightInMeters * heightInMeters)).toFixed(2));
        }
    };

    const applyImperialConversion = () => {
        const convertedHeight = ftImperial ? (parseFloat(ftImperial) * 30.48).toFixed(1) : height;
        const convertedWeight = lbsImperial ? (parseFloat(lbsImperial) * 0.45359237).toFixed(1) : weight;
        setHeight(convertedHeight);
        setWeight(convertedWeight);
        setFtImperial('');
        setLbsImperial('');
        const hm = parseFloat(convertedHeight) / 100;
        const wk = parseFloat(convertedWeight);
        if (hm > 0 && wk > 0) setBmi((wk / (hm * hm)).toFixed(2));
    };

    const labelClass = "font-display text-label font-semibold uppercase tracking-[0.1em] text-slate-zen400 mb-1.5 block";

    return (
        <PageWrapper>
            <div className="max-w-4xl mx-auto">
                <SectionHeader title="Register" description="Create your fighter profile to get started." />

                <div className="bg-parchment-50 border border-[rgba(60,50,40,0.10)] rounded-xl shadow-zen-sm p-6">
                    <form className="space-y-4" onSubmit={handleRegister}>
                        {validationError && (
                            <div className="bg-blood-100 border border-blood-200 rounded-md p-3">
                                {Object.values(validationError.errors).flat().map((error, index) =>
                                    <p key={index} className="font-sans text-sm text-blood-400">{error}</p>
                                )}
                                <p className="font-sans text-xs text-blood-300 mt-1">Check console for details</p>
                            </div>
                        )}

                        <div>
                            <label htmlFor="role" className={labelClass}>Select Role:</label>
                            <Select value={role} onValueChange={setRole}>
                                <SelectTrigger id="role"><SelectValue placeholder="Select role" /></SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="Student">Student</SelectItem>
                                    <SelectItem value="Instructor">Instructor</SelectItem>
                                </SelectContent>
                            </Select>
                        </div>

                        <div>
                            <label htmlFor="name" className={labelClass}>Name:</label>
                            <Input type="text" id="name" value={name} onChange={(e) => setName(e.target.value)} required />
                        </div>

                        <div className="grid grid-cols-2 gap-4">
                            <div>
                                <label htmlFor="height" className={labelClass}>Height:</label>
                                <Input type="text" id="height" value={height} onChange={(e) => setHeight(e.target.value)} onBlur={calculateBmi} required />
                            </div>
                            <div className="flex items-center"><span className="font-sans text-sm text-slate-zen400 ml-2">in CM</span></div>
                            <div>
                                <label htmlFor="weight" className={labelClass}>Weight:</label>
                                <Input type="text" id="weight" value={weight} onChange={(e) => setWeight(e.target.value)} onBlur={calculateBmi} required />
                            </div>
                            <div className="flex items-center"><span className="font-sans text-sm text-slate-zen400 ml-2">in KG</span></div>
                            <div>
                                <label htmlFor="bmi" className={labelClass}>BMI:</label>
                                <Input type="text" id="bmi" value={bmi} readOnly />
                            </div>
                        </div>

                        <div className="border border-dashed border-parchment-300 rounded-lg p-4 mt-4">
                            <h3 className="font-serif text-lg font-semibold text-ink-400 mb-2">Imperial Conversion Tool</h3>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <label htmlFor="imperialHeight" className={labelClass}>Height (FT):</label>
                                    <Input type="text" id="imperialHeight" value={ftImperial} onChange={(e) => setFtImperial(e.target.value)} placeholder="e.g., 5.8" />
                                </div>
                                <div className="flex items-center"><span className="font-sans text-sm text-slate-zen400 ml-2">Converted to CM: {ftImperial ? (parseFloat(ftImperial) * 30.48).toFixed(1) : "0"}</span></div>
                                <div>
                                    <label htmlFor="imperialWeight" className={labelClass}>Weight (LBS):</label>
                                    <Input type="text" id="imperialWeight" value={lbsImperial} onChange={(e) => setLbsImperial(e.target.value)} placeholder="e.g., 150" />
                                </div>
                                <div className="flex items-center"><span className="font-sans text-sm text-slate-zen400 ml-2">Converted to KG: {lbsImperial ? (parseFloat(lbsImperial) * 0.45359237).toFixed(1) : "0"}</span></div>
                            </div>
                            <div className="mt-2">
                                <Button variant="secondary" onClick={applyImperialConversion} size="sm">Apply Conversion</Button>
                            </div>
                        </div>

                        <div>
                            <label htmlFor="gender" className={labelClass}>Gender:</label>
                            <Select value={gender} onValueChange={setGender}>
                                <SelectTrigger id="gender"><SelectValue placeholder="Select gender" /></SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="Male">Male</SelectItem>
                                    <SelectItem value="Female">Female</SelectItem>
                                </SelectContent>
                            </Select>
                        </div>

                        <div>
                            <label htmlFor="birthdate" className={labelClass}>Birthdate:</label>
                            <Input type="date" id="birthdate" value={birthdate} onChange={(e) => setBirthdate(e.target.value)} />
                        </div>

                        <div>
                            <label htmlFor="sparringDuration" className={labelClass}>How long can you spar without breaking? (minutes):</label>
                            <Input type="text" id="sparringDuration" value={sparringDuration} onChange={(e) => setSparringDuration(e.target.value)} required />
                        </div>

                        <div>
                            <label className={labelClass}>Years of Training Experience:</label>
                            <div className="flex space-x-4 font-sans text-sm text-ink-400">
                                <label className="flex items-center gap-1.5">
                                    <input type="radio" name="experience" value={TrainingExperience.LessThanTwoYears} checked={experience === TrainingExperience.LessThanTwoYears} onChange={(e) => setExperience(Number(e.target.value))} className="w-4 h-4 accent-samurai-400" />
                                    Less than 2 years
                                </label>
                                <label className="flex items-center gap-1.5">
                                    <input type="radio" name="experience" value={TrainingExperience.FromTwoToFiveYears} checked={experience === TrainingExperience.FromTwoToFiveYears} onChange={(e) => setExperience(Number(e.target.value))} className="w-4 h-4 accent-samurai-400" />
                                    2 to 5 years
                                </label>
                                <label className="flex items-center gap-1.5">
                                    <input type="radio" name="experience" value={TrainingExperience.MoreThanFiveYears} checked={experience === TrainingExperience.MoreThanFiveYears} onChange={(e) => setExperience(Number(e.target.value))} className="w-4 h-4 accent-samurai-400" />
                                    More than 5 years
                                </label>
                            </div>
                        </div>

                        <div>
                            <label htmlFor="beltRank" className={labelClass}>Brazilian Jiu-Jitsu Belt Rank:</label>
                            <Select value={beltRank} onValueChange={setBeltRank}>
                                <SelectTrigger id="beltRank"><SelectValue placeholder="Select belt rank" /></SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="White">White</SelectItem>
                                    <SelectItem value="Blue">Blue</SelectItem>
                                    <SelectItem value="Purple">Purple</SelectItem>
                                    <SelectItem value="Brown">Brown</SelectItem>
                                    <SelectItem value="Black">Black</SelectItem>
                                </SelectContent>
                            </Select>
                        </div>

                        <div>
                            <label htmlFor="email" className={labelClass}>Email</label>
                            <Input type="email" id="email" name="email" value={email} onChange={(e) => { if (validationError) setValidationError(null); setEmail(e.target.value); }} required />
                        </div>

                        <div>
                            <label htmlFor="password" className={labelClass}>Password</label>
                            <Input type="password" id="password" name="password" value={password} onChange={(e) => { if (validationError) setValidationError(null); setPassword(e.target.value); }} required />
                        </div>

                        <Button type="submit" variant="dark" size="full">Register</Button>
                        <ConfirmationDialog title="Are you sure all of your details are correct?" message="*You cannot update your details once submitted!" isOpen={isDialogOpen} onConfirm={handleConfirm} onCancel={handleCancel} />
                    </form>
                </div>
            </div>
        </PageWrapper>
    );
}
