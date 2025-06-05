import React from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle, CardFooter } from '@/components/ui/card';
import { CheckCircle2 } from 'lucide-react'; // Or any other checkmark icon
import { Link } from 'react-router-dom'; // For "Get Started" buttons

const PricingPage: React.FC = () => {
    return (
        <div className="container mx-auto py-12 px-4 md:px-6">
            <div className="text-center mb-12">
                <h1 className="text-4xl md:text-5xl font-bold text-primary mb-4">
                    Choose Your CodeJitsu Plan
                </h1>
                <p className="text-lg md:text-xl text-muted-foreground max-w-2xl mx-auto">
                    Start revolutionizing your BJJ coaching today. Simple pricing, powerful AI.
                </p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-8 max-w-4xl mx-auto">
                {/* Free Tier Card */}
                <Card className="flex flex-col">
                    <CardHeader className="pb-4">
                        <CardTitle className="text-2xl font-semibold text-center">Free Tier</CardTitle>
                        <CardDescription className="text-center text-muted-foreground h-12">
                            Perfect for getting started and exploring core AI features.
                        </CardDescription>
                        <p className="text-4xl font-bold text-center mt-2">$0<span className="text-lg font-normal text-muted-foreground">/month</span></p>
                    </CardHeader>
                    <CardContent className="flex-grow">
                        <ul className="space-y-3">
                            <li className="flex items-start">
                                <CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-1 flex-shrink-0" />
                                <span>Basic User Authentication & Fighter Profile</span>
                            </li>
                            <li className="flex items-start">
                                <CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-1 flex-shrink-0" />
                                <span><strong>1 Video Upload/Month</strong> for AI Analysis</span>
                            </li>
                            <li className="flex items-start">
                                <CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-1 flex-shrink-0" />
                                <span>View AI-Generated Analysis (Simplified for Free Tier)</span>
                            </li>
                            <li className="flex items-start">
                                <CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-1 flex-shrink-0" />
                                <span><strong>1 Active Class Session</strong> Creation & Management</span>
                            </li>
                            <li className="flex items-start">
                                <CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-1 flex-shrink-0" />
                                <span>Basic AI Curriculum Viewer</span>
                            </li>
                             <li className="flex items-start">
                                <CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-1 flex-shrink-0" />
                                <span>Walk-in Student Attendance</span>
                            </li>
                        </ul>
                    </CardContent>
                    <CardFooter>
                        <Button asChild variant="outline" className="w-full">
                            <Link to="/register">Get Started for Free</Link>
                        </Button>
                    </CardFooter>
                </Card>

                {/* Premium Instructor Tier Card */}
                <Card className="flex flex-col border-primary ring-2 ring-primary">
                    <CardHeader className="pb-4">
                        <div className="flex justify-center mb-2">
                            <span className="inline-block bg-primary text-primary-foreground text-xs font-semibold px-3 py-1 rounded-full uppercase">
                                Early Adopter
                            </span>
                        </div>
                        <CardTitle className="text-2xl font-semibold text-center">Instructor Premium</CardTitle>
                        <CardDescription className="text-center text-muted-foreground h-12">
                            Unlock the full power of AI to supercharge your coaching and dojo.
                        </CardDescription>
                        <p className="text-4xl font-bold text-center mt-2">
                            $10<span className="text-lg font-normal text-muted-foreground">/month</span>
                        </p>
                        <p className="text-sm text-muted-foreground text-center">(Regularly $20/month - Limited Time Offer!)</p>
                    </CardHeader>
                    <CardContent className="flex-grow">
                        <ul className="space-y-3">
                            <li className="flex items-start">
                                <CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-1 flex-shrink-0" />
                                <span>All Free Tier Features, plus:</span>
                            </li>
                            <li className="flex items-start">
                                <CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-1 flex-shrink-0" />
                                <span><strong>Unlimited</strong> Video Uploads & AI Analysis</span>
                            </li>
                            <li className="flex items-start">
                                <CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-1 flex-shrink-0" />
                                <span><strong>Full Video Analysis Editor</strong> Access</span>
                            </li>
                            <li className="flex items-start">
                                <CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-1 flex-shrink-0" />
                                <span><strong>Unlimited</strong> AI-Generated Class Curriculums</span>
                            </li>
                            <li className="flex items-start">
                                <CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-1 flex-shrink-0" />
                                <span><strong>Unlimited</strong> Active Class Session Management</span>
                            </li>
                            <li className="flex items-start">
                                <CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-1 flex-shrink-0" />
                                <span>Advanced Student Pairing Insights</span>
                            </li>
                            <li className="flex items-start">
                                <CheckCircle2 className="h-5 w-5 text-green-500 mr-2 mt-1 flex-shrink-0" />
                                <span>Priority Support</span>
                            </li>
                        </ul>
                    </CardContent>
                    <CardFooter>
                        <Button asChild className="w-full">
                            <Link to="/register?plan=premium">Go Premium & Save 50%</Link>
                        </Button>
                    </CardFooter>
                </Card>
            </div>

            <div className="text-center mt-16">
                <h3 className="text-2xl font-semibold mb-4">Frequently Asked Questions</h3>
                <div className="max-w-2xl mx-auto text-left space-y-4">
                    <div>
                        <h4 className="font-semibold">What happens after the early adopter period?</h4>
                        <p className="text-muted-foreground">
                            Early adopters who sign up for the Instructor Premium plan at $10/month will lock in that price for as long as they maintain their subscription. Future new subscribers will be subject to the standard pricing.
                        </p>
                    </div>
                    <div>
                        <h4 className="font-semibold">Is there a contract or can I cancel anytime?</h4>
                        <p className="text-muted-foreground">
                            Our plans are typically month-to-month, and you can cancel your premium subscription at any time. If you cancel, you'll retain premium access until the end of your current billing period.
                        </p>
                    </div>
                    <div>
                        <h4 className="font-semibold">What about student accounts?</h4>
                        <p className="text-muted-foreground">
                            Students can create free accounts to check into sessions and upload their limited videos. Instructors on the Premium plan allow their linked students to benefit from the instructor's unlimited video analysis quota when videos are submitted through the dojo context (feature details subject to refinement).
                        </p>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default PricingPage;