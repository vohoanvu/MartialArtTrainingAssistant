import React from 'react';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { CalendarCheck, CheckCircle2, Edit3, Target, Video } from 'lucide-react';
import { Link } from 'react-router-dom';

const LandingPage: React.FC = () => {
    return (
        <div className="min-h-screen bg-background text-foreground transition-colors duration-300">
            {/* Hero Section */}
            <section className="bg-primary text-primary-foreground py-20 md:py-28">
                <div className="container mx-auto px-4 text-center">
                    <h1 className="text-4xl md:text-5xl lg:text-6xl font-bold mb-6">
                        Revolutionize Your BJJ Dojo with AI Assistance
                    </h1>
                    <p className="text-lg md:text-xl text-primary-foreground/90 mb-10 max-w-2xl mx-auto">
                        Spend less time planning and analyzing, and more time coaching. CodeJitsu's AI Assistant automates session planning, delivers insightful sparring analysis, and helps you tailor training for impactful student progress.
                    </p>
                    <Button asChild size="lg"
                        className="bg-background text-primary hover:bg-muted hover:text-primary text-lg px-10 py-6 shadow-lg hover:shadow-xl transition-shadow duration-300 transform hover:scale-105"
                    >
                        <a href="/home">Join the AI Revolution – Get Beta Access!</a>
                    </Button>
                    <div className="mt-16 grid grid-cols-1 sm:grid-cols-2 md:grid-cols-2 lg:grid-cols-2 xl:grid-cols-2 justify-center items-center gap-4 md:gap-6">
                        <img
                            src="/mockups/codejitsu-class-dashboard.png" // Main dashboard view focused
                            alt="CodeJitsu AI Class Planning Dashboard"
                            className="w-full max-w-lg rounded-lg shadow-2xl object-cover aspect-video border-4 border-background/20"
                            loading="lazy"
                        />
                        <img
                            src="/mockups/live-video-search.png" // A view of video analysis in progress or results
                            alt="AI-Powered Video Analysis for BJJ"
                            className="w-full max-w-lg rounded-lg shadow-2xl object-cover aspect-video border-4 border-background/20"
                            loading="lazy"
                        />
                    </div>
                </div>
            </section>

            {/* Features Section */}
            <section className="py-16 md:py-24 bg-background">
                <div className="container mx-auto px-4">
                    <h2 className="text-3xl md:text-4xl font-bold text-center mb-16 text-foreground">
                        Empower Your Coaching: How CodeJitsu Works for You
                    </h2>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-8 lg:gap-10 items-start">
                        <Card className="bg-card text-card-foreground shadow-xl border border-border flex flex-col hover:shadow-2xl transition-shadow duration-300">
                            <CardHeader className="text-center pb-4">
                                <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-primary/10 text-primary mb-4">
                                    <CalendarCheck className="h-8 w-8" strokeWidth="2" />
                                </div>
                                <CardTitle className="text-xl font-bold">Smart Class & Lesson Planning</CardTitle>
                                <CardDescription className="text-sm text-muted-foreground min-h-[3rem] md:min-h-[3.5rem]">
                                    Automate attendance, pair students, and get AI-generated lesson plans instantly.
                                </CardDescription>
                            </CardHeader>
                            <CardContent className="flex-grow flex flex-col items-center pt-2">
                                <img
                                    src="/mockups/class-management.png"
                                    alt="AI BJJ Class Planning Interface"
                                    className="w-full h-48 rounded-md object-cover mb-6 shadow-md border border-border/50"
                                    loading="lazy"
                                />
                                <div className="space-y-2 text-sm text-left w-full">
                                    <p className="flex items-start"><CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-0.5 flex-shrink-0" strokeWidth="2.5" /> Walk-in Attendance</p>
                                    <p className="flex items-start"><CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-0.5 flex-shrink-0" strokeWidth="2.5" /> Intelligent Partner Matching</p>
                                    <p className="flex items-start"><CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-0.5 flex-shrink-0" strokeWidth="2.5" /> AI-Drafted Curricula</p>
                                </div>
                            </CardContent>
                        </Card>

                        <Card className="bg-card text-card-foreground shadow-xl border border-border flex flex-col hover:shadow-2xl transition-shadow duration-300">
                            <CardHeader className="text-center pb-4">
                                <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-primary/10 text-primary mb-4">
                                    <Video className="h-8 w-8" strokeWidth="2" />
                                </div>
                                <CardTitle className="text-xl font-bold">Unlock Sparring Insights</CardTitle>
                                <CardDescription className="text-sm text-muted-foreground min-h-[3rem] md:min-h-[3.5rem]">
                                    AI breaks down footage, highlighting techniques, strengths, and areas for growth.
                                </CardDescription>
                            </CardHeader>
                            <CardContent className="flex-grow flex flex-col items-center pt-2">
                                <img
                                    src="/mockups/video-analysis.png"
                                    alt="BJJ Sparring Video Analysis Interface"
                                    className="w-full h-48 rounded-md object-cover mb-6 shadow-md border border-border/50"
                                    loading="lazy"
                                />
                                <div className="space-y-2 text-sm text-left w-full">
                                    <p className="flex items-start"><CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-0.5 flex-shrink-0" strokeWidth="2.5" /> Easy Video Upload</p>
                                    <p className="flex items-start"><CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-0.5 flex-shrink-0" strokeWidth="2.5" /> Key Technique ID</p>
                                    <p className="flex items-start"><CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-0.5 flex-shrink-0" strokeWidth="2.5" /> Editable AI Feedback</p>
                                </div>
                            </CardContent>
                        </Card>

                        <Card className="bg-card text-card-foreground shadow-xl border border-border flex flex-col hover:shadow-2xl transition-shadow duration-300">
                            <CardHeader className="text-center pb-4">
                                <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-primary/10 text-primary mb-4">
                                    <Edit3 className="h-8 w-8" strokeWidth="2" />
                                </div>
                                <CardTitle className="text-xl font-bold">Streamlined Coaching Tools</CardTitle>
                                <CardDescription className="text-sm text-muted-foreground min-h-[3rem] md:min-h-[3.5rem]">
                                    Review AI analysis, make quick edits, and prepare targeted training efficiently.
                                </CardDescription>
                            </CardHeader>
                            <CardContent className="flex-grow flex flex-col items-center pt-2">
                                <img
                                    src="/mockups/ai-analysis-editor.png"
                                    alt="Instructor Editing AI BJJ Feedback"
                                    className="w-full h-48 rounded-md object-cover mb-6 shadow-md border border-border/50"
                                    loading="lazy"
                                />
                                <div className="space-y-2 text-sm text-left w-full">
                                    <p className="flex items-start"><CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-0.5 flex-shrink-0" strokeWidth="2.5" /> Detailed Performance Analysis</p>
                                    <p className="flex items-start"><CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-0.5 flex-shrink-0" strokeWidth="2.5" /> Instant Improvement Plans</p>
                                    <p className="flex items-start"><CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-0.5 flex-shrink-0" strokeWidth="2.5" /> Data-Driven Insights</p>
                                </div>
                            </CardContent>
                        </Card>
                    </div>
                </div>
            </section>

            {/* Testimonials Section */}
            <section className="bg-muted/50 py-16 md:py-24">
                <div className="container mx-auto px-4 text-center">
                    <h2 className="text-3xl md:text-4xl font-bold text-foreground mb-12">Passionate Instructors Love CodeJitsu</h2>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-8 max-w-4xl mx-auto">
                        <Card className="bg-card text-card-foreground shadow-lg border border-border transform transition-transform hover:scale-105">
                            <CardContent className="pt-8 pb-6">
                                <Target className="h-10 w-10 text-primary_text-muted-foreground mx-auto mb-4" />
                                <p className="italic text-muted-foreground text-lg">"CodeJitsu's AI video analysis is a game-changer. I can pinpoint student mistakes and strengths much faster, giving me more time to actually coach. The AI-generated curriculum ideas are a fantastic starting point for my classes."</p>
                                <p className="mt-6 font-semibold text-foreground text-sm">— Coach at a Leading National BJJ Competition Team</p>
                            </CardContent>
                        </Card>
                        <Card className="bg-card text-card-foreground shadow-lg border border-border transform transition-transform hover:scale-105">
                            <CardContent className="pt-8 pb-6">
                                <Target className="h-10 w-10 text-primary_text-muted-foreground mx-auto mb-4" />
                                <p className="italic text-muted-foreground text-lg">"Managing class attendance, especially with walk-ins, used to be chaotic. CodeJitsu streamlined it. Plus, generating tailored drills with the AI assistant keeps my students engaged and learning effectively."</p>
                                <p className="mt-6 font-semibold text-foreground text-sm">— Owner & Head Instructor, Thriving Community Dojo</p>
                            </CardContent>
                        </Card>
                    </div>
                </div>
            </section>

           {/* Beta Launch & Value Props Section */}
            <section id="beta-launch" className="bg-gradient-to-br from-primary/5 via-background to-secondary/5 py-16 md:py-24">
                <div className="container mx-auto px-4">
                    <div className="text-center mb-12 md:mb-16">
                        <h2 className="text-3xl md:text-4xl font-bold text-primary mb-4">
                            Ready to Transform Your BJJ Dojo? CodeJitsu Beta is LIVE!
                        </h2>
                        <p className="text-lg md:text-xl text-muted-foreground max-w-3xl mx-auto mb-4">
                            Step into the future of BJJ coaching. Our AI-powered assistant helps create a thriving, feedback-driven, and beginner-friendly training environment that keeps students coming back.
                        </p>
                    </div>

                    <div className="grid md:grid-cols-2 gap-8 lg:gap-12 items-center mb-12 md:mb-16">
                        <div className="space-y-6 text-left">
                            <h3 className="text-2xl font-semibold text-foreground">For Instructors: Multiply Your Impact, Minimize Hassle</h3>
                            <p className="text-muted-foreground">
                                Effortlessly design engaging, structured classes for all skill levels. CodeJitsuAI suggests drills and lesson plans, letting you focus on safe, motivating teaching. Data-driven insights and streamlined feedback help students skyrocket their progress, boosting your dojo's retention and reputation.
                            </p>
                            <ul className="space-y-2">
                                <li className="flex items-center"><CheckCircle2 className="h-5 w-5 text-green-500 mr-2 flex-shrink-0" strokeWidth="2.5" /><span className="text-foreground">AI-Generated Lesson Plans: Tailored & Fresh.</span></li>
                                <li className="flex items-center"><CheckCircle2 className="h-5 w-5 text-green-500 mr-2 flex-shrink-0" strokeWidth="2.5" /><span className="text-foreground">Reduced Prep Time: More for your students.</span></li>
                                <li className="flex items-center"><CheckCircle2 className="h-5 w-5 text-green-500 mr-2 flex-shrink-0" strokeWidth="2.5" /><span className="text-foreground">Enhanced Student Progress & Retention.</span></li>
                            </ul>
                        </div>
                        <div>
                            <img
                                src="/mockups/ai-lessons-1.png"
                                alt="CodeJitsu Instructor AI Suite"
                                className="rounded-xl shadow-2xl border-2 border-border/30"
                                loading="lazy"
                            />
                            <p className="text-xs text-center text-muted-foreground mt-2 hidden">
                            (Visual: Instructor dashboard with lesson plan, video analytics, and pairing.)
                            </p>
                        </div>
                    </div>

                    <div className="grid md:grid-cols-2 gap-8 lg:gap-12 items-center">
                        <div className="md:order-2 space-y-6 text-left">
                            <h3 className="text-2xl font-semibold text-foreground">For Students: Train Smarter, Connect Deeper</h3>
                            <p className="text-muted-foreground">
                                No more awkward pairing! Our AI Partner Matching finds suitable training partners, making sessions less stressful and more productive for all, especially beginners and kids. Enjoy fun, engaging training, build friendships, and make BJJ accessible and rewarding for everyone.
                            </p>
                            <ul className="space-y-2">
                                <li className="flex items-center"><CheckCircle2 className="h-5 w-5 text-green-500 mr-2 flex-shrink-0" strokeWidth="2.5" /><span className="text-foreground">AI Partner Matching: Safe & effective partners.</span></li>
                                <li className="flex items-center"><CheckCircle2 className="h-5 w-5 text-green-500 mr-2 flex-shrink-0" strokeWidth="2.5" /><span className="text-foreground">Fun, Engaging, Stress-Reduced Learning.</span></li>
                                <li className="flex items-center"><CheckCircle2 className="h-5 w-5 text-green-500 mr-2 flex-shrink-0" strokeWidth="2.5" /><span className="text-foreground">Accelerated Improvement with Personalize Lessons.</span></li>
                            </ul>
                        </div>
                        <div  className="md:order-1">
                            <img
                                src="/mockups/ai-lessons-3.png"
                                alt="AI Partner Matching for BJJ Students"
                                className="rounded-xl shadow-2xl border-2 border-border/30"
                                loading="lazy"
                            />
                            <p className="text-xs text-center text-muted-foreground mt-2 hidden">
                                (Visual: Students happily paired for drilling/sparring via AI feature.)
                            </p>
                        </div>
                    </div>

                    <div className="text-center pt-12 md:pt-20_border-t border-border/20 my-10">
                        <h3 className="text-2xl md:text-3xl font-bold text-primary mb-4">
                            Join Our Beta & Get Generous FREE Premium Features!
                        </h3>
                        <p className="text-muted-foreground mb-8 max-w-xl mx-auto">
                            Be an early shaper of CodeJitsu! Sign up for our Beta Program today and receive extended access to our AI lesson planning and video analysis tools on our Free plan*. Your feedback is invaluable!
                        </p>
                         <div className="space-y-4 sm:space-y-0 sm:space-x-4 flex flex-col sm:flex-row justify-center">
                            <Button asChild size="lg" className="bg-primary text-primary-foreground hover:bg-primary/90 text-lg px-8 py-3 shadow-lg transform hover:scale-105 transition-transform">
                                <Link to="/register?role=Instructor">Get Started as Instructor (Free Beta)</Link>
                            </Button>
                            {/* <Button asChild size="lg" variant="secondary" className="text-secondary-foreground hover:bg-secondary/80 text-lg px-8 py-3 shadow-lg transform hover:scale-105 transition-transform">
                                <Link to="/register?role=Student">Join as a Student (Free Beta)</Link>
                            </Button> */}
                        </div>
                        <p className="text-xs text-muted-foreground mt-4">*Limited spots for enhanced Beta Free Plan. Check our <Link to="/pricing" className="underline hover:text-primary">Pricing Page</Link> for details after Beta.</p>
                    </div>
                </div>
            </section>

            {/* Footer */}
            <footer className="bg-muted/30 border-t border-border text-muted-foreground py-8">
                <div className="container mx-auto px-4 text-center">
                    <p className="mb-2 text-sm">Empowering BJJ Instructors & Students Worldwide with AI</p>
                    <p className="text-xs">© {new Date().getFullYear()} CodeJitsu. All rights reserved.</p>
                    <div className="mt-4 space-x-4">
                        <Link to="/privacy" className="hover:underline text-xs_hover:text-primary">Privacy Policy</Link>
                        <Link to="/contact" className="hover:underline text-xs_hover:text-primary">Contact Us</Link>
                    </div>
                </div>
            </footer>
        </div>
    );
};

export default LandingPage;


// const [email, setEmail] = useState('');
// const [role, setRole] = useState('Instructor');
// const [region, setRegion] = useState('');
// const [isSubmitting, setIsSubmitting] = useState(false);
// const { toast } = useToast();

// const handleSubmit = async (e: React.FormEvent) => {
//     e.preventDefault();
//     setIsSubmitting(true);

//     try {
//         const resp = await joinWailList({ email, role, region });
//         if (resp == 200) {
//             toast({
//                 title: "You’re on the List!",
//                 description: "Thank you for joining. You’ll be among the first to experience our AI Assistant—keep an eye on your inbox for early access!",
//             });
//             setEmail('');
//             setRole('');
//             setRegion('');
//         } else {
//             toast({
//                 title: 'Error',
//                 description: 'Failed to join waitlist. Please try again.',
//                 variant: 'destructive',
//             });
//         }
//     } catch (error) {
//         toast({
//             title: 'Error',
//             description: 'Failed to join waitlist. Please try again.',
//             variant: 'destructive',
//         });
//     } finally {
//         setIsSubmitting(false);
//     }
// };
{/* Waitlist Section */}
{/* <section id="waitlist" className="bg-muted py-16">
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
                alt="AI Generated Lesson planning"
                className="w-full max-w-md rounded-lg shadow-lg"
                loading="lazy"
            />
            <img
                src="/mockups/ai-lessons-2.png"
                alt="AI Generated Lesson planning"
                className="w-full max-w-md rounded-lg shadow-lg"
                loading="lazy"
            />
            <img
                src="/mockups/ai-lessons-3.png"
                alt="AI Generated Lesson planning"
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
</section> */}