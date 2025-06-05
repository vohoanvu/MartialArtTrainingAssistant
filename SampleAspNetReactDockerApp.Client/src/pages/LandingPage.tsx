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
                        Imagine having a dedicated AI Assistant that plans your classes day-to-day activities, analyzes sparring footage, and personalizes learning for every student—streamlining your dojo operations and boosting retention like never before.
                    </p>
                    <Button asChild className="bg-card text-primary hover:bg-accent hover:text-accent-foreground text-lg px-8 py-3 shadow">
                        <a href="#waitlist">Join the AI Revolution in BJJ Training – Get Started Today!</a>
                    </Button>
                    <div className="mt-12 flex flex-col md:flex-row justify-center items-center gap-8">
                        <img
                            src="/mockups/codejitsu-class-dashboard.png"
                            alt="Class Dashboard"
                            className="w-[480px] h-[300px] rounded-lg shadow-lg object-cover"
                            loading="lazy"
                        />
                        <img
                            src="/mockups/live-video-search.png"
                            alt="Live Video Search"
                            className="w-[480px] h-[300px] rounded-lg shadow-lg object-cover"
                            loading="lazy"
                        />
                    </div>
                </div>
            </section>

            {/* Features Section */}
            <section className="py-16">
                <div className="container mx-auto px-4">
                    <h2 className="text-3xl font-bold text-center mb-12">
                        Empower Your Coaching: How CodeJitsu's AI Works for You
                    </h2>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-8 items-start">
                        {/* Feature Card 1: Smart Class & Curriculum */}
                        <Card className="bg-card text-card-foreground shadow-lg border border-border flex flex-col">
                            <CardHeader className="text-center">
                                <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-primary text-primary-foreground mb-4">
                                    {/* Icon Example: Users or Calendar */}
                                    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="lucide lucide-calendar-check"><rect width="18" height="18" x="3" y="4" rx="2" ry="2"/><line x1="16" x2="16" y1="2" y2="6"/><line x1="8" x2="8" y1="2" y2="6"/><line x1="3" x2="21" y1="10" y2="10"/><path d="m9 16 2 2 4-4"/></svg>
                                </div>
                                <CardTitle>Smart Class & Lesson Planning</CardTitle>
                                <CardDescription className="text-sm">
                                    Automate attendance, pair students, and get AI-generated lesson plans instantly.
                                </CardDescription>
                            </CardHeader>
                            <CardContent className="flex-grow flex flex-col items-center">
                                <img
                                    src="/mockups/class-management.png"
                                    alt="AI BJJ Class Planning"
                                    className="w-full h-48 rounded-md object-cover mb-4 shadow-md"
                                    loading="lazy"
                                />
                                <div className="space-y-2 text-sm text-center">
                                    <p><span className="font-semibold text-primary">✓</span> Walk-in Attendance</p>
                                    <p><span className="font-semibold text-primary">✓</span> Intelligent Pairing</p>
                                    <p><span className="font-semibold text-primary">✓</span> AI-Drafted Curricula</p>
                                </div>
                            </CardContent>
                        </Card>

                        {/* Feature Card 2: AI Sparring Analysis */}
                        <Card className="bg-card text-card-foreground shadow-lg border border-border flex flex-col">
                            <CardHeader className="text-center">
                                <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-primary text-primary-foreground mb-4">
                                    {/* Icon Example: Video or Bar Chart */}
                                    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="lucide lucide-video"><path d="m22 8-6 4 6 4V8Z"/><rect width="14" height="12" x="2" y="6" rx="2" ry="2"/></svg>
                                </div>
                                <CardTitle>Unlock Sparring Insights</CardTitle>
                                <CardDescription className="text-sm">
                                    AI breaks down footage, highlighting techniques, strengths, and areas for growth.
                                </CardDescription>
                            </CardHeader>
                            <CardContent className="flex-grow flex flex-col items-center">
                                <img
                                    src="/mockups/video-analysis.png"
                                    alt="BJJ Sparring Video Analysis Interface"
                                    className="w-full h-48 rounded-md object-cover mb-4 shadow-md"
                                    loading="lazy"
                                />
                                <div className="space-y-2 text-sm text-center">
                                    <p><span className="font-semibold text-primary">✓</span> Easy Video Upload</p>
                                    <p><span className="font-semibold text-primary">✓</span> Key Technique identification</p>
                                    <p><span className="font-semibold text-primary">✓</span> Editable AI Feedback</p>
                                </div>
                            </CardContent>
                        </Card>

                        {/* Feature Card 3: Efficient Coaching Workflow */}
                        <Card className="bg-card text-card-foreground shadow-lg border border-border flex flex-col">
                            <CardHeader className="text-center">
                                <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-primary text-primary-foreground mb-4">
                                    {/* Icon Example: Edit or Clipboard Check */}
                                    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="lucide lucide-clipboard-check"><rect width="8" height="4" x="8" y="2" rx="1" ry="1"/><path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"/><path d="m9 14 2 2 4-4"/></svg>
                                </div>
                                <CardTitle>Streamlined Coaching Tools</CardTitle>
                                <CardDescription className="text-sm">
                                    Review AI analysis, make quick edits, and prepare targeted training efficiently.
                                </CardDescription>
                            </CardHeader>
                            <CardContent className="flex-grow flex flex-col items-center">
                                <img
                                    src="/mockups/ai-analysis-editor.png"
                                    alt="Instructor Editing AI Feedback"
                                    className="w-full h-48 rounded-md object-cover mb-4 shadow-md"
                                    loading="lazy"
                                />
                                <div className="space-y-2 text-sm text-center">
                                    <p><span className="font-semibold text-primary">✓</span> Focused Analysis Review</p>
                                    <p><span className="font-semibold text-primary">✓</span> Quick-Edit Interface</p>
                                    <p><span className="font-semibold text-primary">✓</span> Data-Driven Insights</p>
                                </div>
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
                                <p className="italic">"CodeJitsu's AI video analysis is a game-changer. I can pinpoint student mistakes and strengths much faster, giving me more time to actually coach. The AI-generated curriculum ideas are a fantastic starting point for my classes."</p>
                                <p className="mt-4 font-semibold">— Renowned BJJ Black Belt & Competition Coach</p>
                            </CardContent>
                        </Card>
                        <Card>
                            <CardContent className="pt-6">
                                <p className="italic">"Managing class attendance, especially with walk-ins, used to be chaotic. CodeJitsu streamlined it. Plus, generating tailored drills with the AI assistant keeps my students engaged and learning effectively."</p>
                                <p className="mt-4 font-semibold">— Owner & Head Instructor</p>
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
                        Get early access to CodeJitsu and our exclusive MVP launch discount. Help shape the future of BJJ training while making your coaching more efficient and impactful.
                    </p>
                    <div className="flex flex-col md:flex-row justify-center items-center gap-8 mb-8">
                        <img
                            src="/mockups/ai-lessons-1.png"
                            alt="AI Generated Lesson plans"
                            className="w-full max-w-md rounded-lg shadow-lg"
                            loading="lazy"
                        />
                        <img
                            src="/mockups/ai-lessons-2.png"
                            alt="AI Generated Lesson plans"
                            className="w-full max-w-md rounded-lg shadow-lg"
                            loading="lazy"
                        />
                        <img
                            src="/mockups/ai-lessons-3.png"
                            alt="AI Generated Lesson plans"
                            className="w-full max-w-md rounded-lg shadow-lg"
                            loading="lazy"
                        />
                    </div>
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