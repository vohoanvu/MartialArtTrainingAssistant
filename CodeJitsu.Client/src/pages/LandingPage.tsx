import React from 'react';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { CalendarCheck, CheckCircle2, Edit3, Target, Video } from 'lucide-react';
import { Link } from 'react-router-dom';
import { HeroSection } from '@/components/zen/HeroSection';
import { PageWrapper } from '@/components/zen/PageWrapper';
import { SectionHeader } from '@/components/zen/SectionHeader';
import { ZenDivider } from '@/components/zen/ZenDivider';

const LandingPage: React.FC = () => {
    return (
        <div className="min-h-screen bg-parchment-100">
            {/* Hero Section */}
            <HeroSection
                eyebrow="BJJ Dojo Management · AI-Powered"
                title="Revolutionize Your BJJ Dojo with AI Assistance"
                subtitle="Spend less time planning and analyzing, and more time coaching. CodeJitsu's AI Assistant automates session planning, delivers insightful sparring analysis, and helps you tailor training for impactful student progress."
                ctaLabel="Join the AI Revolution — Get Beta Access!"
                onCta={() => window.location.href = '/home'}
            >
                {/* Video embed */}
                <div className="mt-8 md:mt-12 max-w-4xl mx-auto">
                    <div className="aspect-video rounded-xl shadow-zen-lg overflow-hidden bg-slate-zen600">
                        <iframe
                            className="w-full h-full"
                            src="https://www.youtube.com/embed/RGXiLOPkxGY?autoplay=0&modestbranding=1&rel=0&showinfo=0&controls=1"
                            title="CodeJitsu BJJ AI Assistant Demo"
                            frameBorder="0"
                            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
                            referrerPolicy="strict-origin-when-cross-origin"
                            allowFullScreen
                        ></iframe>
                    </div>
                </div>
            </HeroSection>

            {/* Features Section */}
            <PageWrapper>
                <SectionHeader
                    centered
                    title="Empower Your Coaching: How CodeJitsu Works for You"
                />
                <div className="grid grid-cols-1 md:grid-cols-3 gap-5 mt-6">
                    <Card className="flex flex-col hover:shadow-zen-md hover:-translate-y-px transition-all duration-base ease-zen">
                        <CardHeader className="text-center pb-3">
                            <div className="mx-auto flex items-center justify-center h-14 w-14 rounded-full bg-samurai-100 text-samurai-400 mb-3">
                                <CalendarCheck className="h-7 w-7" strokeWidth={1.5} />
                            </div>
                            <CardTitle>Smart Class & Lesson Planning</CardTitle>
                            <CardDescription>
                                Automate attendance, pair students, and get AI-generated lesson plans instantly.
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="flex-grow flex flex-col items-center pt-2">
                            <img
                                src="/mockups/class-management.png"
                                alt="AI BJJ Class Planning Interface"
                                className="w-full h-48 rounded-lg object-cover mb-5 shadow-zen-sm border border-[rgba(60,50,40,0.10)]"
                                loading="lazy"
                            />
                            <div className="space-y-2 text-sm text-left w-full font-sans text-ink-400">
                                <p className="flex items-start"><CheckCircle2 className="h-4 w-4 text-samurai-400 mr-2 mt-0.5 flex-shrink-0" strokeWidth={2} /> Walk-in Attendance</p>
                                <p className="flex items-start"><CheckCircle2 className="h-4 w-4 text-samurai-400 mr-2 mt-0.5 flex-shrink-0" strokeWidth={2} /> Intelligent Partner Matching</p>
                                <p className="flex items-start"><CheckCircle2 className="h-4 w-4 text-samurai-400 mr-2 mt-0.5 flex-shrink-0" strokeWidth={2} /> AI-Drafted Curricula</p>
                            </div>
                        </CardContent>
                    </Card>

                    <Card className="flex flex-col hover:shadow-zen-md hover:-translate-y-px transition-all duration-base ease-zen">
                        <CardHeader className="text-center pb-3">
                            <div className="mx-auto flex items-center justify-center h-14 w-14 rounded-full bg-samurai-100 text-samurai-400 mb-3">
                                <Video className="h-7 w-7" strokeWidth={1.5} />
                            </div>
                            <CardTitle>Unlock Sparring Insights</CardTitle>
                            <CardDescription>
                                AI breaks down footage, highlighting techniques, strengths, and areas for growth.
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="flex-grow flex flex-col items-center pt-2">
                            <img
                                src="/mockups/video-analysis.png"
                                alt="BJJ Sparring Video Analysis Interface"
                                className="w-full h-48 rounded-lg object-cover mb-5 shadow-zen-sm border border-[rgba(60,50,40,0.10)]"
                                loading="lazy"
                            />
                            <div className="space-y-2 text-sm text-left w-full font-sans text-ink-400">
                                <p className="flex items-start"><CheckCircle2 className="h-4 w-4 text-samurai-400 mr-2 mt-0.5 flex-shrink-0" strokeWidth={2} /> Easy Video Upload</p>
                                <p className="flex items-start"><CheckCircle2 className="h-4 w-4 text-samurai-400 mr-2 mt-0.5 flex-shrink-0" strokeWidth={2} /> Key Technique ID</p>
                                <p className="flex items-start"><CheckCircle2 className="h-4 w-4 text-samurai-400 mr-2 mt-0.5 flex-shrink-0" strokeWidth={2} /> Editable AI Feedback</p>
                            </div>
                        </CardContent>
                    </Card>

                    <Card className="flex flex-col hover:shadow-zen-md hover:-translate-y-px transition-all duration-base ease-zen">
                        <CardHeader className="text-center pb-3">
                            <div className="mx-auto flex items-center justify-center h-14 w-14 rounded-full bg-samurai-100 text-samurai-400 mb-3">
                                <Edit3 className="h-7 w-7" strokeWidth={1.5} />
                            </div>
                            <CardTitle>Streamlined Coaching Tools</CardTitle>
                            <CardDescription>
                                Review AI analysis, make quick edits, and prepare targeted training efficiently.
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="flex-grow flex flex-col items-center pt-2">
                            <img
                                src="/mockups/ai-analysis-editor.png"
                                alt="Instructor Editing AI BJJ Feedback"
                                className="w-full h-48 rounded-lg object-cover mb-5 shadow-zen-sm border border-[rgba(60,50,40,0.10)]"
                                loading="lazy"
                            />
                            <div className="space-y-2 text-sm text-left w-full font-sans text-ink-400">
                                <p className="flex items-start"><CheckCircle2 className="h-4 w-4 text-samurai-400 mr-2 mt-0.5 flex-shrink-0" strokeWidth={2} /> Detailed Performance Analysis</p>
                                <p className="flex items-start"><CheckCircle2 className="h-4 w-4 text-samurai-400 mr-2 mt-0.5 flex-shrink-0" strokeWidth={2} /> Instant Improvement Plans</p>
                                <p className="flex items-start"><CheckCircle2 className="h-4 w-4 text-samurai-400 mr-2 mt-0.5 flex-shrink-0" strokeWidth={2} /> Data-Driven Insights</p>
                            </div>
                        </CardContent>
                    </Card>
                </div>
            </PageWrapper>

            <ZenDivider label="Testimonials" />

            {/* Testimonials Section */}
            <PageWrapper className="bg-parchment-200/30">
                <SectionHeader centered title="Passionate Instructors Love CodeJitsu" />
                <div className="grid grid-cols-1 md:grid-cols-2 gap-5 max-w-4xl mx-auto mt-6">
                    <Card className="hover:shadow-zen-md transition-all duration-base ease-zen">
                        <CardContent className="pt-6 pb-5">
                            <Target className="h-8 w-8 text-samurai-400 mx-auto mb-4" />
                            <p className="italic font-serif text-sm text-slate-zen500 leading-relaxed">"CodeJitsu's AI video analysis is a game-changer. I can pinpoint student mistakes and strengths much faster, giving me more time to actually coach. The AI-generated curriculum ideas are a fantastic starting point for my classes."</p>
                            <p className="mt-5 font-sans text-sm font-semibold text-ink-400">— Coach at a Leading National BJJ Competition Team</p>
                        </CardContent>
                    </Card>
                    <Card className="hover:shadow-zen-md transition-all duration-base ease-zen">
                        <CardContent className="pt-6 pb-5">
                            <Target className="h-8 w-8 text-samurai-400 mx-auto mb-4" />
                            <p className="italic font-serif text-sm text-slate-zen500 leading-relaxed">"Managing class attendance, especially with walk-ins, used to be chaotic. CodeJitsu streamlined it. Plus, generating tailored drills with the AI assistant keeps my students engaged and learning effectively."</p>
                            <p className="mt-5 font-sans text-sm font-semibold text-ink-400">— Owner & Head Instructor, Thriving Community Dojo</p>
                        </CardContent>
                    </Card>
                </div>
            </PageWrapper>

            <ZenDivider />

            {/* Beta Launch Section */}
            <PageWrapper>
                <SectionHeader
                    centered
                    eyebrow="Beta Launch"
                    title="Ready to Transform Your BJJ Dojo?"
                    description="Step into the future of BJJ coaching. Our AI-powered assistant helps create a thriving, feedback-driven, and beginner-friendly training environment."
                />

                <div className="grid md:grid-cols-2 gap-6 items-center mt-8">
                    <div className="space-y-5 text-left">
                        <h3 className="font-serif text-xl font-semibold text-ink-400">For Instructors: Multiply Your Impact</h3>
                        <p className="font-sans text-sm text-slate-zen400 leading-relaxed">
                            Effortlessly design engaging, structured classes for all skill levels. CodeJitsuAI suggests drills and lesson plans, letting you focus on safe, motivating teaching.
                        </p>
                        <ul className="space-y-2 font-sans text-sm text-ink-400">
                            <li className="flex items-center"><CheckCircle2 className="h-4 w-4 text-samurai-400 mr-2 flex-shrink-0" strokeWidth={2} />AI-Generated Lesson Plans: Tailored & Fresh.</li>
                            <li className="flex items-center"><CheckCircle2 className="h-4 w-4 text-samurai-400 mr-2 flex-shrink-0" strokeWidth={2} />Reduced Prep Time: More for your students.</li>
                            <li className="flex items-center"><CheckCircle2 className="h-4 w-4 text-samurai-400 mr-2 flex-shrink-0" strokeWidth={2} />Enhanced Student Progress & Retention.</li>
                        </ul>
                    </div>
                    <div>
                        <img
                            src="/mockups/ai-lessons-1.png"
                            alt="CodeJitsu Instructor AI Suite"
                            className="rounded-xl shadow-zen-md border border-[rgba(60,50,40,0.10)]"
                            loading="lazy"
                        />
                    </div>
                </div>

                <div className="grid md:grid-cols-2 gap-6 items-center mt-8">
                    <div className="md:order-2 space-y-5 text-left">
                        <h3 className="font-serif text-xl font-semibold text-ink-400">For Students: Train Smarter, Connect Deeper</h3>
                        <p className="font-sans text-sm text-slate-zen400 leading-relaxed">
                            No more awkward pairing! Our AI Partner Matching finds suitable training partners, making sessions less stressful and more productive.
                        </p>
                        <ul className="space-y-2 font-sans text-sm text-ink-400">
                            <li className="flex items-center"><CheckCircle2 className="h-4 w-4 text-samurai-400 mr-2 flex-shrink-0" strokeWidth={2} />AI Partner Matching: Safe & effective partners.</li>
                            <li className="flex items-center"><CheckCircle2 className="h-4 w-4 text-samurai-400 mr-2 flex-shrink-0" strokeWidth={2} />Fun, Engaging, Stress-Reduced Learning.</li>
                            <li className="flex items-center"><CheckCircle2 className="h-4 w-4 text-samurai-400 mr-2 flex-shrink-0" strokeWidth={2} />Accelerated Improvement with Personalized Lessons.</li>
                        </ul>
                    </div>
                    <div className="md:order-1">
                        <img
                            src="/mockups/ai-lessons-3.png"
                            alt="AI Partner Matching for BJJ Students"
                            className="rounded-xl shadow-zen-md border border-[rgba(60,50,40,0.10)]"
                            loading="lazy"
                        />
                    </div>
                </div>

                <div className="text-center mt-12">
                    <h3 className="font-serif text-2xl font-bold text-ink-400 mb-3">
                        Join Our Beta & Get Generous FREE Premium Features!
                    </h3>
                    <p className="font-sans text-sm text-slate-zen400 mb-6 max-w-xl mx-auto">
                        Be an early shaper of CodeJitsu! Sign up for our Beta Program today and receive extended access to our AI lesson planning and video analysis tools on our Free plan.
                    </p>
                    <Button variant="dark" size="lg" asChild>
                        <Link to="/register?role=Instructor">Get Started as Instructor (Free Beta)</Link>
                    </Button>
                    <p className="font-sans text-xs text-slate-zen300 mt-3">*Limited spots for enhanced Beta Free Plan. Check our <Link to="/pricing" className="underline text-samurai-400 hover:text-samurai-500">Pricing Page</Link> for details after Beta.</p>
                </div>
            </PageWrapper>

            {/* Footer */}
            <footer className="bg-slate-zen700 border-t border-white/5 text-parchment-300 py-7">
                <div className="max-w-[1120px] mx-auto px-5 text-center">
                    <p className="font-sans text-sm mb-2">Empowering BJJ Instructors & Students Worldwide with AI</p>
                    <p className="font-sans text-xs text-parchment-400">&copy; {new Date().getFullYear()} CodeJitsu. All rights reserved.</p>
                    <div className="mt-3 space-x-4">
                        <Link to="/privacy" className="font-sans text-xs text-parchment-300 hover:text-parchment-100 transition-colors duration-fast ease-zen">Privacy Policy</Link>
                        <Link to="/contact" className="font-sans text-xs text-parchment-300 hover:text-parchment-100 transition-colors duration-fast ease-zen">Contact Us</Link>
                    </div>
                </div>
            </footer>
        </div>
    );
};

export default LandingPage;
