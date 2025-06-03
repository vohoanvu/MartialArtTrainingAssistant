//import { Button } from '@/components/ui/button';
import { CurriculumDto } from '@/types/global';
import DrillTimer from './DrillTimer';
import { useState } from 'react';

interface CurriculumSectionProps {
    curriculum: CurriculumDto;
    onFeedback: (helpful: boolean) => void;
}

const CurriculumSection = ({ curriculum }: CurriculumSectionProps) => {
    const [expandedSections, setExpandedSections] = useState<{ [key: string]: boolean }>({});

    const toggleAccordion = (id: string) => {
        setExpandedSections((prev) => ({
            ...prev,
            [id]: !prev[id]
        }));
    };

    function formatDescriptionWithNumberedList(text: string) {
        const numberedListRegex = /(\d+\.\s[^.]+(?:\.[^0-9]|$))/g;
        const matches = text.match(numberedListRegex);

        if (matches && matches.length > 1) {
            const firstNumberIndex = text.search(/\d+\.\s/);
            const beforeList = text.slice(0, firstNumberIndex).trim();
            const listItems = text
                .slice(firstNumberIndex)
                .split(/\d+\.\s/)
                .filter(Boolean)
                .map(item => item.replace(/^\s*|\s*\.$/g, '').trim());

            return (
                <div>
                    {beforeList && <span>{beforeList}</span>}
                    <ol className="list-decimal ml-6">
                        {listItems.map((item, idx) => (
                            <li key={idx}>{item}</li>
                        ))}
                    </ol>
                </div>
            );
        }
        return <span>{text}</span>;
    }

    return (
        <div className="mt-8">
            <h2 className="text-3xl font-bold mb-4 text-center">{curriculum.session_title}</h2>
            <p className="text-lg text-muted-foreground mb-6 text-center">Total Duration: {curriculum.duration}</p>

            {/* Navigation Bar */}
            <nav className="flex flex-wrap justify-center space-x-4 mb-6 bg-accent text-accent-foreground p-2 rounded-lg sticky top-0 z-10 shadow">
                <a href="#warm-up" className="hover:underline focus:underline transition-colors">Warm-Up</a>
                <a href="#techniques" className="hover:underline focus:underline transition-colors">Techniques</a>
                <a href="#drills" className="hover:underline focus:underline transition-colors">Drills</a>
                <a href="#sparring" className="hover:underline focus:underline transition-colors">Sparring</a>
                <a href="#cool-down" className="hover:underline focus:underline transition-colors">Cool-Down</a>
            </nav>

            {/* Warm-Up */}
            <section id="warm-up" className="mb-6">
                <div className="bg-orange-100 dark:bg-orange-900 p-4 rounded-lg shadow">
                    <div className="flex items-center justify-between">
                        <h3 className="text-2xl font-semibold text-orange-800 dark:text-orange-200">Warm-Up</h3>
                    </div>
                    <p className="text-lg font-bold mt-2">{curriculum.warm_up.name}</p>
                    <p className="text-muted-foreground">{formatDescriptionWithNumberedList(curriculum.warm_up.description)}</p>
                    <p className="text-sm text-muted-foreground">Duration: {curriculum.warm_up.duration}</p>
                    <DrillTimer
                        initialDurationMinutes={parseInt(curriculum.warm_up.duration.split(' ')[0]) || 10}
                        drillName={curriculum.warm_up.name}
                        themeColor="bg-orange-500"
                        progressColor="bg-orange-600"
                    />
                </div>
            </section>

            {/* Techniques */}
            <section id="techniques" className="mb-6">
                <div className="bg-blue-100 dark:bg-blue-900 p-4 rounded-lg shadow">
                    <h3 className="text-2xl font-semibold text-blue-800 dark:text-blue-200">Techniques</h3>
                    <div className="mt-2">
                        {curriculum.techniques.map((tech, index) => (
                            <div key={index} className="border-b border-blue-200 dark:border-blue-800 py-2">
                                <button
                                    className="w-full text-left text-lg font-bold text-blue-700 dark:text-blue-300 flex justify-between items-center focus:outline-none"
                                    onClick={() => toggleAccordion(`tech-${index}`)}
                                >
                                    {tech.name}
                                    <span>{expandedSections[`tech-${index}`] ? '▲' : '▼'}</span>
                                </button>
                                <div className={`mt-2 ${expandedSections[`tech-${index}`] ? 'block' : 'hidden'}`}>
                                    {formatDescriptionWithNumberedList(tech.description)}
                                    <p className="relative group">
                                        <span className="font-semibold">Tips: </span>
                                        <span className="underline decoration-dotted cursor-help group-hover:no-underline">{tech.tips}</span>
                                        <span className="absolute hidden group-hover:block bg-background text-foreground text-sm rounded p-2 -mt-10 w-64 shadow-lg z-20">
                                            {tech.tips}
                                        </span>
                                    </p>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            </section>

            {/* Drills */}
            <section id="drills" className="mb-6">
                <div className="bg-green-100 dark:bg-green-900 p-4 rounded-lg shadow">
                    <h3 className="text-2xl font-semibold text-green-800 dark:text-green-200">Drills</h3>
                    <div className="mt-2">
                        {curriculum.drills.map((drill, index) => (
                            <div key={index} className="border-b border-green-200 dark:border-green-800 py-2">
                                <button
                                    className="w-full text-left text-lg font-bold text-green-700 dark:text-green-300 flex justify-between items-center focus:outline-none"
                                    onClick={() => toggleAccordion(`drill-${index}`)}
                                >
                                    {drill.name}
                                    <span>{expandedSections[`drill-${index}`] ? '▲' : '▼'}</span>
                                </button>
                                <div className={`mt-2 ${expandedSections[`drill-${index}`] ? 'block' : 'hidden'}`}>
                                    <p>{drill.description}</p>
                                    <p><strong>Focus:</strong> {drill.focus}</p>
                                    <p className="text-sm text-muted-foreground">Duration: {drill.duration}</p>
                                    <DrillTimer
                                        initialDurationMinutes={parseInt(drill.duration.split(' ')[0]) || 5}
                                        drillName={drill.name}
                                        themeColor="bg-green-500"
                                        progressColor="bg-green-600"
                                    />
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            </section>

            {/* Sparring */}
            <section id="sparring" className="mb-6">
                <div className="bg-red-100 dark:bg-red-900 p-4 rounded-lg shadow">
                    <div className="flex items-center justify-between">
                        <h3 className="text-2xl font-semibold text-red-800 dark:text-red-200">Sparring</h3>
                    </div>
                    <p className="text-lg font-bold mt-2">{curriculum.sparring.name}</p>
                    {formatDescriptionWithNumberedList(curriculum.sparring.description)}
                    <p className="font-semibold">Guidelines:</p>
                    <ul className="list-disc ml-6">
                        {(curriculum.sparring.guidelines || '').split(';').map((guideline, index) => (
                            <li key={index}>{guideline.trim()}</li>
                        ))}
                    </ul>
                    <p className="text-sm text-muted-foreground">Duration: {curriculum.sparring.duration}</p>
                    <DrillTimer
                        initialDurationMinutes={parseInt(curriculum.sparring.duration.split(' ')[0]) || 15}
                        drillName={curriculum.sparring.name}
                        themeColor="bg-red-500"
                        progressColor="bg-red-600"
                    />
                </div>
            </section>

            {/* Cool-Down */}
            <section id="cool-down" className="mb-6">
                <div className="bg-purple-100 dark:bg-purple-900 p-4 rounded-lg shadow">
                    <div className="flex items-center justify-between">
                        <h3 className="text-2xl font-semibold text-purple-800 dark:text-purple-200">Cool-Down</h3>
                    </div>
                    <p className="text-lg font-bold mt-2">{curriculum.cool_down.name}</p>
                    <p className="text-muted-foreground">{curriculum.cool_down.description}</p>
                    <p className="text-sm text-muted-foreground">Duration: {curriculum.cool_down.duration}</p>
                    <DrillTimer
                        initialDurationMinutes={parseInt(curriculum.cool_down.duration.split(' ')[0]) || 10}
                        drillName={curriculum.cool_down.name}
                        themeColor="bg-purple-500"
                        progressColor="bg-purple-600"
                    />
                </div>
            </section>

            {/* Feedback Section */}
            {/* <section id="feedback" className="mb-6">
                <div className="bg-accent p-4 rounded-lg shadow">
                    <h3 className="text-2xl font-semibold">Feedback</h3>
                    <p className="text-lg mt-2">Was this helpful?</p>
                    <div className="flex space-x-4 mt-2">
                        <Button
                            onClick={() => onFeedback(true)}
                            className="bg-green-600 hover:bg-green-700 text-white"
                        >
                            Yes
                        </Button>
                        <Button
                            onClick={() => onFeedback(false)}
                            className="bg-red-600 hover:bg-red-700 text-white"
                        >
                            No
                        </Button>
                    </div>
                    <textarea
                        className="w-full h-24 p-2 mt-4 border rounded bg-background border-border focus:ring-2 focus:ring-primary"
                        placeholder="Share your feedback or suggestions. You can record observations or adjustments during the session..."
                    ></textarea>
                    <Button className="mt-2 bg-primary hover:bg-primary/90 text-primary-foreground">
                        Submit Feedback
                    </Button>
                </div>
            </section> */}
        </div>
    );
};

export default CurriculumSection;