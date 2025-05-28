import React, { useState } from 'react';
import { Button } from '../components/ui/button';
import { Input } from '../components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../components/ui/select';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { useToast } from '../hooks/use-toast';
import { joinWailList } from '../services/api';

const LandingPage: React.FC = () => {
    const [email, setEmail] = useState('');
    const [role, setRole] = useState('Instructor');
    const [region, setRegion] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const { toast } = useToast();

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSubmitting(true);
    
        try {
            const resp = await joinWailList({ email, role, region });
            if (resp == 200) {
                toast({
                    title: "You’re on the List!",
                    description: "Thank you for joining. You’ll be among the first to experience our AI Assistant—keep an eye on your inbox for early access!",
                });
                setEmail('');
                setRole('');
                setRegion('');
            } else {
                toast({
                    title: 'Error',
                    description: 'Failed to join waitlist. Please try again.',
                    variant: 'destructive',
                });
            }
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
                        Revolutionize Your BJJ Dojo with AI Assistance
                    </h1>
                    <p className="text-lg md:text-xl mb-8 max-w-2xl mx-auto">
                        Imagine having a dedicated AI Assistant that manages your classes, analyzes sparring footage, and personalizes learning for every student—streamlining your dojo operations and boosting retention like never before.
                    </p>
                    <Button
                        asChild
                        className="bg-card text-primary hover:bg-accent hover:text-accent-foreground text-lg px-8 py-3 shadow"
                    >
                        <a href="#waitlist">Join the AI Revolution in BJJ Training – Get Started Today!</a>
                    </Button>
                    <div className="mt-12">
                        {/* <img
                            src="/mockups/ai-dashboard.png"
                            alt="AI Assistant Dashboard"
                            className="mx-auto w-full max-w-3xl rounded-lg shadow-lg"
                            loading="lazy"
                        /> */}
                    </div>
                </div>
            </section>

            {/* Features Section */}
            <section className="py-16">
                <div className="container mx-auto px-4">
                    <h2 className="text-3xl font-bold text-center mb-12">
                        Your AI Assistant: Streamline Operations, Enhance Learning
                    </h2>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
                        <Card className="bg-card text-card-foreground shadow-lg border border-border">
                            <CardHeader>
                                <CardTitle>Effortless Class Management</CardTitle>
                                <CardDescription>
                                    Let your AI Assistant oversee daily dojo activities like a pro.
                                </CardDescription>
                            </CardHeader>
                            <CardContent>
                                {/* <img
                                    src="/mockups/class-management.png"
                                    alt="Class Management Dashboard"
                                    className="w-32 h-24 rounded-md object-cover"
                                    loading="lazy"
                                /> */}
                                <ul className="list-disc pl-5 mt-2">
                                    <li>Automate attendance and pair students by size and skill.</li>
                                    <li>Generate tailored training curriculums for each class.</li>
                                    <li>Track sparring and drilling time effortlessly.</li>
                                </ul>
                            </CardContent>
                        </Card>
                        <Card className="bg-card text-card-foreground shadow-lg border border-border">
                            <CardHeader>
                                <CardTitle>In-Depth Sparring Analysis</CardTitle>
                                <CardDescription>
                                    Turn footage into actionable insights with AI precision.
                                </CardDescription>
                            </CardHeader>
                            <CardContent>
                                {/* <img
                                    src="/mockups/sparring-analysis.png"
                                    alt="Sparring Analysis Report"
                                    className="w-32 h-24 rounded-md object-cover"
                                    loading="lazy"
                                /> */}
                                <ul className="list-disc pl-5 mt-2">
                                    <li>Upload footage for AI-driven breakdowns.</li>
                                    <li>Get feedback on techniques, strengths, and improvements.</li>
                                    <li>Use it as a template for personalized coaching.</li>
                                </ul>
                            </CardContent>
                        </Card>
                        <Card className="bg-card text-card-foreground shadow-lg border border-border">
                            <CardHeader>
                                <CardTitle>Personalized Learning Journeys</CardTitle>
                                <CardDescription>
                                    Extend your expertise to every student, effortlessly.
                                </CardDescription>
                            </CardHeader>
                            <CardContent>
                                {/* <img
                                    src="/mockups/personalized-lessons.png"
                                    alt="Personalized Feedback Distribution"
                                    className="w-32 h-24 rounded-md object-cover"
                                    loading="lazy"
                                /> */}
                                <ul className="list-disc pl-5 mt-2">
                                    <li>Students upload videos for your review.</li>
                                    <li>AI distributes your approved feedback via email.</li>
                                    <li>Curated resources enhance student engagement.</li>
                                </ul>
                            </CardContent>
                        </Card>
                    </div>
                </div>
            </section>

            {/* Testimonials Section */}
            <section className="bg-muted py-16">
                <div className="container mx-auto px-4 text-center">
                    <h2 className="text-3xl font-bold mb-8">What Instructors Are Saying</h2>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-8 max-w-4xl mx-auto">
                        <Card>
                            <CardContent className="pt-6">
                                <p className="italic">"This AI Assistant saved me hours of planning and brought more students to my dojo!"</p>
                                <p className="mt-4 font-semibold">— Coach Alex, USA</p>
                            </CardContent>
                        </Card>
                        <Card>
                            <CardContent className="pt-6">
                                <p className="italic">"Student retention jumped 30% since we started using personalized feedback."</p>
                                <p className="mt-4 font-semibold">— Sensei Maria, Brazil</p>
                            </CardContent>
                        </Card>
                    </div>
                </div>
            </section>

            {/* Waitlist Section */}
            <section id="waitlist" className="bg-muted py-16">
                <div className="container mx-auto px-4">
                    <h2 className="text-3xl font-bold text-center mb-8">
                        Be the First to Experience the Future of AI-assisted BJJ coaching
                    </h2>
                    <p className="text-lg text-center mb-8 max-w-xl mx-auto">
                        Join BJJ instructors worldwide transforming their dojos with AI. Sign up now and unlock exclusive early access!
                    </p>
                    {/* <img
                        src="/mockups/ai-curriculum.png"
                        alt="AI Curriculum Dashboard"
                        className="mx-auto w-full max-w-2xl rounded-lg shadow-lg"
                        loading="lazy"
                    /> */}
                    <Card className="max-w-md mx-auto bg-card text-card-foreground shadow-lg border border-border">
                        <CardHeader>
                            <CardTitle>Early Access to Your AI Assistant</CardTitle>
                            <CardDescription>
                                Shape the future of BJJ with cutting-edge tools designed for you.
                            </CardDescription>
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
                                            <SelectItem value="Instructor">BJJ Instructor</SelectItem>
                                            <SelectItem value="Student">BJJ Student</SelectItem>
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
                                            <SelectItem value="South America">South America</SelectItem>
                                            <SelectItem value="Europe">Europe</SelectItem>
                                            <SelectItem value="Asia">Asia</SelectItem>
                                            <SelectItem value="Africa">Africa</SelectItem>
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
                    <p className="mb-2">Empowering BJJ Instructors Worldwide with AI</p>
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