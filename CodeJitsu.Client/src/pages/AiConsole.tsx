import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Cpu, Lock, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { FeatureConfigCard } from '@/components/AiConsole/FeatureConfigCard';
import { QuotaConfigCard } from '@/components/AiConsole/QuotaConfigCard';
import { AiConsoleOverview, AiFeatureConfig } from '@/components/AiConsole/types';
import { AiConsoleApiError, getAiConsoleOverview } from '@/services/api';
import useAuthStore from '@/store/authStore';

type PageState =
    | { status: 'loading' }
    | { status: 'forbidden' }
    | { status: 'error'; message: string }
    | { status: 'ready'; overview: AiConsoleOverview };

const AiConsole: React.FC = () => {
    const { t } = useTranslation();
    const accessToken = useAuthStore((s) => s.accessToken);
    const hydrate = useAuthStore((s) => s.hydrate);
    const [state, setState] = useState<PageState>({ status: 'loading' });
    const [reloadKey, setReloadKey] = useState(0);

    useEffect(() => {
        let cancelled = false;
        getAiConsoleOverview({ jwtToken: accessToken, hydrate })
            .then((overview) => {
                if (!cancelled) setState({ status: 'ready', overview });
            })
            .catch((e: unknown) => {
                if (cancelled) return;
                if (e instanceof AiConsoleApiError && e.status === 403) {
                    setState({ status: 'forbidden' });
                } else {
                    setState({ status: 'error', message: e instanceof Error ? e.message : t('aiConsole.error.body') });
                }
            });
        return () => {
            cancelled = true;
        };
        // hydrate/t are stable enough; reloadKey drives manual retries.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [accessToken, reloadKey]);

    const retry = useCallback(() => {
        setState({ status: 'loading' });
        setReloadKey((k) => k + 1);
    }, []);

    const handleUpdated = useCallback((config: AiFeatureConfig) => {
        setState((prev) =>
            prev.status === 'ready'
                ? {
                      status: 'ready',
                      overview: {
                          ...prev.overview,
                          features: prev.overview.features.map((f) => (f.key === config.key ? config : f)),
                      },
                  }
                : prev
        );
    }, []);

    return (
        <div className="mx-auto w-full max-w-[1120px] px-4 py-6 md:px-5">
            <header className="mb-6">
                <h1 className="flex items-center gap-2 font-serif text-2xl font-semibold text-ink-400">
                    <Cpu className="h-6 w-6 text-samurai-400" aria-hidden="true" />
                    {t('aiConsole.title')}
                </h1>
                <p className="mt-2 max-w-3xl font-sans text-sm text-slate-zen400">{t('aiConsole.subtitle')}</p>
            </header>

            {state.status === 'loading' && (
                <div className="space-y-4" role="status" aria-label={t('aiConsole.loading')} data-testid="ai-console-loading">
                    {[0, 1, 2].map((i) => (
                        <div key={i} className="space-y-3 rounded-xl border border-[rgba(60,50,40,0.10)] bg-parchment-50 p-5">
                            <Skeleton className="h-6 w-1/3" />
                            <Skeleton className="h-4 w-2/3" />
                            <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
                                <Skeleton className="h-10" />
                                <Skeleton className="h-10" />
                            </div>
                        </div>
                    ))}
                    <span className="sr-only">{t('aiConsole.loading')}</span>
                </div>
            )}

            {state.status === 'forbidden' && (
                <Alert variant="warning" role="alert" className="flex gap-3">
                    <Lock className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
                    <div>
                        <AlertTitle>{t('aiConsole.noAccess.title')}</AlertTitle>
                        <AlertDescription>{t('aiConsole.noAccess.body')}</AlertDescription>
                    </div>
                </Alert>
            )}

            {state.status === 'error' && (
                <Alert variant="destructive" role="alert">
                    <AlertTitle>{t('aiConsole.error.title')}</AlertTitle>
                    <AlertDescription className="space-y-3">
                        <p className="break-words">{state.message}</p>
                        <Button type="button" variant="secondary" size="sm" onClick={retry}>
                            <RefreshCw className="h-3.5 w-3.5" aria-hidden="true" />
                            {t('aiConsole.error.retry')}
                        </Button>
                    </AlertDescription>
                </Alert>
            )}

            {state.status === 'ready' && (
                <div className="space-y-6">
                    <QuotaConfigCard jwtToken={accessToken} hydrate={hydrate} />
                    {state.overview.features.length === 0 && (
                        <p className="text-sm text-slate-zen400">{t('aiConsole.empty')}</p>
                    )}
                    {state.overview.features.map((feature) => (
                        <FeatureConfigCard
                            key={feature.key}
                            feature={feature}
                            models={state.overview.models}
                            jwtToken={accessToken}
                            hydrate={hydrate}
                            onUpdated={handleUpdated}
                        />
                    ))}
                </div>
            )}
        </div>
    );
};

export default AiConsole;
