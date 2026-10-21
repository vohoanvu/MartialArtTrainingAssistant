import React from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { ArrowUpRight, BadgeX, CalendarCheck, CalendarDays, CheckCircle2, ClipboardCheck, CreditCard, FlaskConical, HeartHandshake, Search, Smartphone, TriangleAlert, Trophy, Video } from 'lucide-react';
import { HeroSection } from '@/components/zen/HeroSection';
import { PageWrapper } from '@/components/zen/PageWrapper';
import { SectionHeader } from '@/components/zen/SectionHeader';
import { ZenDivider } from '@/components/zen/ZenDivider';

const STATUS_ITEMS = [
    { key: 'notForSale', Icon: BadgeX },
    { key: 'internal', Icon: FlaskConical },
    { key: 'asIs', Icon: TriangleAlert },
] as const;

const FEATURES = [
    { key: 'analysis', Icon: Video },
    { key: 'planning', Icon: CalendarCheck },
    { key: 'search', Icon: Search },
] as const;

const TKNQ_FEATURES = [
    { key: 'billing', Icon: CreditCard },
    { key: 'attendance', Icon: ClipboardCheck },
    { key: 'classes', Icon: CalendarDays },
    { key: 'leaderboard', Icon: Trophy },
    { key: 'apps', Icon: Smartphone },
    { key: 'retention', Icon: HeartHandshake },
] as const;

const RND_POINTS = ['p1', 'p2', 'p3', 'p4'] as const;

// Every outbound TKNQ link is UTM-tagged so referrals from this demo show up in TKNQ's analytics,
// with utm_content naming the placement that was clicked.
const tknqUrl =(placement: string) =>
    `https://tknq.io/?utm_source=codejitsu&utm_medium=referral&utm_campaign=demo_landing&utm_content=${placement}`;

// Technology names are proper nouns, so they are not translated.
const STACK = ['Gemini 4 Argon', 'Vertex AI', 'Google Search grounding', 'YouTube Data API', '.NET 10', 'React 19', 'Supabase PostgreSQL', 'Google Cloud'];

const LandingPage: React.FC = () => {
    const { t } = useTranslation();

    return (
        <div className="min-h-screen bg-parchment-100">
            {/* Hero: the demo video */}
            <HeroSection
                eyebrow={t('landing.hero.eyebrow')}
                title={t('landing.hero.title')}
                subtitle={t('landing.hero.subtitle')}
                ctaLabel={t('landing.hero.cta')}
                onCta={() => window.location.href = '/home'}
                secondaryCta={{ label: t('landing.hero.secondaryCta'), href: tknqUrl('hero') }}
            >
                <div className="mt-8 md:mt-12 max-w-4xl mx-auto">
                    <div className="aspect-video rounded-xl shadow-zen-lg overflow-hidden bg-slate-zen600 border border-white/10">
                        <video
                            className="w-full h-full object-cover"
                            src="/videos/codejitsu-demo.mp4"
                            poster="/videos/codejitsu-demo-poster.jpg"
                            title={t('landing.hero.videoTitle')}
                            aria-label={t('landing.hero.videoTitle')}
                            controls
                            playsInline
                            preload="metadata"
                        >
                            {t('landing.hero.videoFallback')}
                        </video>
                    </div>
                </div>
            </HeroSection>

            {/* Demo / R&D status */}
            <PageWrapper className="min-h-0 py-8">
                <SectionHeader
                    centered
                    eyebrow={t('landing.status.eyebrow')}
                    title={t('landing.status.title')}
                />
                <div className="grid grid-cols-1 md:grid-cols-3 gap-5 mt-6">
                    {STATUS_ITEMS.map(({ key, Icon }) => (
                        <Card key={key} className="border-l-4 border-l-gold-accent">
                            <CardHeader className="pb-2">
                                <div className="flex items-center gap-3">
                                    <Icon className="h-6 w-6 text-gold-text flex-shrink-0" strokeWidth={1.5} />
                                    <CardTitle>{t(`landing.status.${key}.title`)}</CardTitle>
                                </div>
                            </CardHeader>
                            <CardContent>
                                <p className="font-sans text-sm text-slate-zen500 leading-relaxed">{t(`landing.status.${key}.body`)}</p>
                            </CardContent>
                        </Card>
                    ))}
                </div>
            </PageWrapper>

            <ZenDivider />

            {/* What the demo does */}
            <PageWrapper className="min-h-0 py-8">
                <SectionHeader
                    centered
                    title={t('landing.features.title')}
                    description={t('landing.features.description')}
                />
                <div className="grid grid-cols-1 md:grid-cols-3 gap-5 mt-6">
                    {FEATURES.map(({ key, Icon }) => (
                        <Card key={key} className="flex flex-col hover:shadow-zen-md hover:-translate-y-px transition-all duration-base ease-zen">
                            <CardHeader className="text-center pb-3">
                                <div className="mx-auto flex items-center justify-center h-14 w-14 rounded-full bg-samurai-100 text-samurai-400 mb-3">
                                    <Icon className="h-7 w-7" strokeWidth={1.5} />
                                </div>
                                <CardTitle>{t(`landing.features.${key}.title`)}</CardTitle>
                                <CardDescription>{t(`landing.features.${key}.body`)}</CardDescription>
                            </CardHeader>
                            <CardContent className="flex-grow pt-2">
                                <div className="space-y-2 text-sm text-left w-full font-sans text-ink-400">
                                    {(['b1', 'b2', 'b3'] as const).map((b) => (
                                        <p key={b} className="flex items-start">
                                            <CheckCircle2 className="h-4 w-4 text-samurai-400 mr-2 mt-0.5 flex-shrink-0" strokeWidth={2} />
                                            {t(`landing.features.${key}.${b}`)}
                                        </p>
                                    ))}
                                </div>
                            </CardContent>
                        </Card>
                    ))}
                </div>
            </PageWrapper>

            {/* The company behind the demo */}
            <section className="bg-slate-zen700 text-parchment-100 py-12 md:py-16 relative overflow-hidden">
                <div className="absolute inset-0 bg-[radial-gradient(ellipse_70%_60%_at_80%_20%,rgba(201,162,39,0.12),transparent)]" />
                <div className="relative max-w-[1120px] mx-auto px-5">
                    <div className="text-center max-w-3xl mx-auto">
                        <p className="font-display text-label font-semibold uppercase tracking-[0.2em] text-gold-accent mb-3">{t('landing.tknq.eyebrow')}</p>
                        <h2 className="font-serif text-3xl md:text-4xl font-bold text-parchment-50 mb-4">{t('landing.tknq.title')}</h2>
                        <p className="font-sans text-base text-parchment-300 leading-relaxed">{t('landing.tknq.description')}</p>
                    </div>
                    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 mt-10">
                        {TKNQ_FEATURES.map(({ key, Icon }) => (
                            <div key={key} className="rounded-xl border border-white/10 bg-white/[0.04] p-5">
                                <div className="flex items-center gap-3 mb-2">
                                    <Icon className="h-5 w-5 text-gold-accent flex-shrink-0" strokeWidth={1.75} />
                                    <h3 className="font-serif text-lg font-semibold text-parchment-50">{t(`landing.tknq.${key}.title`)}</h3>
                                </div>
                                <p className="font-sans text-sm text-parchment-300 leading-relaxed">{t(`landing.tknq.${key}.body`)}</p>
                            </div>
                        ))}
                    </div>
                    <div className="text-center mt-10">
                        <Button variant="cta" size="lg" asChild>
                            <a href={tknqUrl('spotlight')} target="_blank" rel="noopener noreferrer">
                                {t('landing.tknq.cta')}
                                <ArrowUpRight className="h-4 w-4 ml-2" strokeWidth={2} />
                            </a>
                        </Button>
                        <p className="font-sans text-xs text-parchment-400 mt-3">{t('landing.tknq.note')}</p>
                    </div>
                </div>
            </section>

            {/* Why it exists: internal R&D */}
            <PageWrapper className="min-h-0 py-8 bg-parchment-200/30">
                <SectionHeader
                    centered
                    eyebrow={t('landing.rnd.eyebrow')}
                    title={t('landing.rnd.title')}
                    description={t('landing.rnd.description')}
                />
                <div className="max-w-3xl mx-auto mt-6 space-y-3">
                    {RND_POINTS.map((p) => (
                        <p key={p} className="flex items-start font-sans text-sm text-ink-400 leading-relaxed">
                            <FlaskConical className="h-4 w-4 text-samurai-400 mr-3 mt-0.5 flex-shrink-0" strokeWidth={2} />
                            {t(`landing.rnd.${p}`)}
                        </p>
                    ))}
                </div>
                <div className="max-w-3xl mx-auto mt-8 text-center">
                    <h3 className="font-serif text-lg font-semibold text-ink-400 mb-3">{t('landing.rnd.stackTitle')}</h3>
                    <div className="flex flex-wrap justify-center gap-2">
                        {STACK.map((s) => (
                            <span key={s} className="font-mono text-xs px-3 py-1.5 rounded-full bg-parchment-50 border border-[rgba(60,50,40,0.18)] text-slate-zen500">{s}</span>
                        ))}
                    </div>
                </div>
            </PageWrapper>

            {/* Try it */}
            <PageWrapper className="min-h-0 py-8">
                <div className="text-center">
                    <h2 className="font-serif text-2xl font-bold text-ink-400 mb-3">{t('landing.cta.title')}</h2>
                    <p className="font-sans text-sm text-slate-zen400 mb-6 max-w-xl mx-auto">{t('landing.cta.body')}</p>
                    <Button variant="dark" size="lg" asChild>
                        <Link to="/home">{t('landing.cta.button')}</Link>
                    </Button>
                    <div className="mt-8 pt-6 border-t border-[rgba(60,50,40,0.12)] max-w-xl mx-auto">
                        <p className="font-serif text-lg font-semibold text-ink-400 mb-2">{t('landing.cta.tknqPrompt')}</p>
                        <a
                            href={tknqUrl('closing')}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="inline-flex items-center font-sans text-sm font-semibold text-samurai-400 hover:text-samurai-500 transition-colors duration-fast ease-zen"
                        >
                            {t('landing.cta.tknqButton')}
                            <ArrowUpRight className="h-4 w-4 ml-1" strokeWidth={2} />
                        </a>
                    </div>
                </div>
            </PageWrapper>

            {/* Footer */}
            <footer className="bg-slate-zen700 border-t border-white/5 text-parchment-300 py-7">
                <div className="max-w-[1120px] mx-auto px-5 text-center">
                    <p className="font-sans text-sm mb-2">{t('landing.footer.line')}</p>
                    <p className="font-sans text-xs text-parchment-400">{t('landing.footer.copyright', { year: new Date().getFullYear() })}</p>
                    <div className="mt-3 space-x-4">
                        <a href={tknqUrl('footer')} target="_blank" rel="noopener noreferrer" className="font-sans text-xs text-gold-accent hover:text-parchment-100 transition-colors duration-fast ease-zen">{t('landing.footer.visit')}</a>
                        <Link to="/privacy" className="font-sans text-xs text-parchment-300 hover:text-parchment-100 transition-colors duration-fast ease-zen">{t('landing.footer.privacy')}</Link>
                        <Link to="/contact" className="font-sans text-xs text-parchment-300 hover:text-parchment-100 transition-colors duration-fast ease-zen">{t('landing.footer.contact')}</Link>
                    </div>
                </div>
            </footer>
        </div>
    );
};

export default LandingPage;
