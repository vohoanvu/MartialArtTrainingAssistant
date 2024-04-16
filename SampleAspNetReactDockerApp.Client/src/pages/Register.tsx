// Register.tsx
import {useState} from "react";
import {useNavigate} from "react-router-dom";
import useAuthStore from "@/store/authStore.ts";
import {Button} from "@/components/ui/button.tsx";

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

export default function Register() {
    const navigate = useNavigate();
    // Assuming the existence of a 'register' method in useAuthStore similar to 'login'
    const register = useAuthStore((state) => state.register);

    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [validationError, setValidationError] = useState<ValidationError | null>();

    const handleRegister = async (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        try {
            const success = await register(email, password);
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
            </form>
        </div>
    );
}
