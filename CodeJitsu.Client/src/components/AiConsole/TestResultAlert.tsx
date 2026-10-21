import { useTranslation } from 'react-i18next';
import { CheckCircle2, XCircle } from 'lucide-react';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { AiModelTestResult } from './types';

interface TestResultAlertProps {
    result: AiModelTestResult | null;
    /** Transport/HTTP failure (the test endpoint itself could not be reached). */
    requestError: string | null;
}

export function TestResultAlert({ result, requestError }: TestResultAlertProps) {
    const { t } = useTranslation();

    if (requestError) {
        return (
            <Alert variant="destructive" role="alert" className="flex gap-3">
                <XCircle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
                <div>
                    <AlertTitle>{t('aiConsole.test.failedTitle')}</AlertTitle>
                    <AlertDescription>{requestError}</AlertDescription>
                </div>
            </Alert>
        );
    }

    if (!result) return null;

    if (result.ok) {
        return (
            <Alert variant="success" role="status" className="flex gap-3">
                <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
                <div className="min-w-0">
                    <AlertTitle>{t('aiConsole.test.okTitle')}</AlertTitle>
                    <AlertDescription className="space-y-1">
                        <p>
                            {t('aiConsole.test.latency', { ms: result.latencyMs })}
                            {result.responseModelVersion
                                ? ` · ${t('aiConsole.test.modelVersion', { version: result.responseModelVersion })}`
                                : ''}
                        </p>
                        {result.sample && (
                            <blockquote className="break-words whitespace-pre-wrap border-l-2 border-emerald-300 pl-3 italic">
                                {result.sample}
                            </blockquote>
                        )}
                    </AlertDescription>
                </div>
            </Alert>
        );
    }

    return (
        <Alert variant="destructive" role="alert" className="flex gap-3">
            <XCircle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
            <div className="min-w-0">
                <AlertTitle>{t('aiConsole.test.failedTitle')}</AlertTitle>
                <AlertDescription className="space-y-1">
                    <p>{t('aiConsole.test.latency', { ms: result.latencyMs })}</p>
                    <p className="break-words">{result.error ?? t('aiConsole.test.unknownError')}</p>
                </AlertDescription>
            </div>
        </Alert>
    );
}
