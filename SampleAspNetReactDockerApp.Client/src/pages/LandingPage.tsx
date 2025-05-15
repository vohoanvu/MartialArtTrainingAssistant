import React, { useState } from 'react';
import { Button } from '../components/ui/button';
import { Input } from '../components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../components/ui/select';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { useToast } from '../hooks/use-toast';
import { joinWailList } from '../services/api';

const LandingPage: React.FC = () => {
    const [email, setEmail] = useState('');
    const [role, setRole] = useState('');
    const [region, setRegion] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const { toast } = useToast();

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSubmitting(true);

        try {
            await joinWailList({ email, role, region });
            toast({
                title: 'Success!',
                description: 'You have joined the waitlist. Expect a 50% discount on launch!',
            });
            setEmail('');
            setRole('');
            setRegion('');
        } catch (error) {
            toast({
                title: 'Error',
                description: 'Failed to join waitlist. Please try again.',
                variant: 'destructive',
            });
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="min-h-screen bg-background text-foreground transition-colors duration-300">
            {/* Hero Section */}
            <section className="bg-primary text-primary-foreground py-20">
                <div className="container mx-auto px-4 text-center">
                    <h1 className="text-4xl md:text-5xl font-bold mb-4">
                        Revolutionize Your BJJ Training with AI
                    </h1>
                    <p className="text-lg md:text-xl mb-8 max-w-2xl mx-auto">
                        CodeJitsu empowers instructors and students with AI-driven video analysis, class management, and personalized curriculum recommendations.
                    </p>
                    <Button
                        asChild
                        className="bg-card text-primary hover:bg-accent hover:text-accent-foreground text-lg px-8 py-3 shadow"
                    >
                        <a href="#waitlist">Join the Waitlist for 50% Off</a>
                    </Button>
                </div>
            </section>

            {/* Features Section */}
            <section className="py-16">
                <div className="container mx-auto px-4">
                    <h2 className="text-3xl font-bold text-center mb-12">
                        Why Choose CodeJitsu as your BJJ Training Assistant?
                    </h2>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
                        <Card className="bg-card text-card-foreground shadow-lg border border-border">
                            <CardHeader>
                                <CardTitle>AI Video Analysis</CardTitle>
                                <CardDescription>
                                    Upload sparring videos for instant AI-driven feedback on techniques, strengths, and improvements.
                                </CardDescription>
                            </CardHeader>
                            <CardContent>
                                <p>Powered by Google Vertex AI, get precise insights to elevate your training.</p>
                            </CardContent>
                        </Card>
                        <Card className="bg-card text-card-foreground shadow-lg border border-border">
                            <CardHeader>
                                <CardTitle>Class Management</CardTitle>
                                <CardDescription>
                                    Create sessions, pair students by size and skill, and manage attendance effortlessly.
                                </CardDescription>
                            </CardHeader>
                            <CardContent>
                                <p>Streamline your dojo operations with intuitive tools.</p>
                            </CardContent>
                        </Card>
                        <Card className="bg-card text-card-foreground shadow-lg border border-border">
                            <CardHeader>
                                <CardTitle>AI Curriculum Recommendations</CardTitle>
                                <CardDescription>
                                    Receive tailored drill plans based on your students’ weaknesses and skill levels.
                                </CardDescription>
                            </CardHeader>
                            <CardContent>
                                <p>Optimize every class with data-driven training plans.</p>
                            </CardContent>
                        </Card>
                    </div>
                </div>
            </section>

            {/* Waitlist Section */}
            <section id="waitlist" className="bg-muted py-16">
                <div className="container mx-auto px-4">
                    <h2 className="text-3xl font-bold text-center mb-8">
                        Join Our Beta Waitlist
                    </h2>
                    <p className="text-lg text-center mb-8 max-w-xl mx-auto">
                        Be the first to experience our MVP and enjoy a 50% discount on the premium tier at launch!
                    </p>
                    <Card className="max-w-md mx-auto bg-card text-card-foreground shadow-lg border border-border">
                        <CardHeader>
                            <CardTitle>Sign Up Now</CardTitle>
                            <CardDescription>Secure your spot and help shape the future of BJJ training.</CardDescription>
                        </CardHeader>
                        <CardContent>
                            <form onSubmit={handleSubmit} className="space-y-4">
                                <div>
                                    <label htmlFor="email" className="block text-sm font-medium mb-1">Email Address</label>
                                    <Input
                                        id="email"
                                        type="email"
                                        value={email}
                                        onChange={(e) => setEmail(e.target.value)}
                                        required
                                        placeholder="your.email@example.com"
                                        className="bg-background text-foreground"
                                    />
                                </div>
                                <div>
                                    <label htmlFor="role" className="block text-sm font-medium mb-1">Your Role</label>
                                    <Select value={role} onValueChange={setRole} required>
                                        <SelectTrigger className="bg-background text-foreground">
                                            <SelectValue placeholder="Select your role" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="Instructor">Instructor</SelectItem>
                                            <SelectItem value="Student">Student</SelectItem>
                                        </SelectContent>
                                    </Select>
                                </div>
                                <div>
                                    <label htmlFor="region" className="block text-sm font-medium mb-1">Region</label>
                                    <Select value={region} onValueChange={setRegion} required>
                                        <SelectTrigger className="bg-background text-foreground">
                                            <SelectValue placeholder="Select your region" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="North America">North America</SelectItem>
                                            <SelectItem value="Europe">Europe</SelectItem>
                                            <SelectItem value="Asia">Asia</SelectItem>
                                            <SelectItem value="Other">Other</SelectItem>
                                        </SelectContent>
                                    </Select>
                                </div>
                                <Button
                                    type="submit"
                                    className="w-full"
                                    disabled={isSubmitting}
                                >
                                    {isSubmitting ? 'Joining...' : 'Join Waitlist'}
                                </Button>
                            </form>
                        </CardContent>
                    </Card>
                </div>
            </section>

            {/* Footer */}
            <footer className="bg-card border-t border-border text-muted-foreground py-8">
                <div className="container mx-auto px-4 text-center">
                    <p>&copy; 2025 Martial Art Training Assistant. All rights reserved.</p>
                    <div className="mt-4 space-x-4">
                        <a href="/privacy" className="hover:underline">Privacy Policy</a>
                        <a href="/contact" className="hover:underline">Contact Us</a>
                    </div>
                </div>
            </footer>
        </div>
    );
};

export default LandingPage;