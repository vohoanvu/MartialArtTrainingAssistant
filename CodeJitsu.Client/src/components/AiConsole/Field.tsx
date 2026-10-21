import { ReactNode } from 'react';
import { cn } from '@/lib/utils';

interface FieldProps {
    id: string;
    label: string;
    hint?: ReactNode;
    error?: string;
    className?: string;
    children: ReactNode;
}

/** Label + control + hint/error text wrapper. The control should use `id`; hint id is `${id}-hint`, error id `${id}-error`. */
export function Field({ id, label, hint, error, className, children }: FieldProps) {
    return (
        <div className={cn('flex flex-col gap-1.5', className)}>
            <label htmlFor={id} className="font-sans text-xs font-medium text-ink-300">
                {label}
            </label>
            {children}
            {hint && (
                <p id={`${id}-hint`} className="text-xs text-slate-zen300">
                    {hint}
                </p>
            )}
            {error && (
                <p id={`${id}-error`} role="alert" className="text-xs text-blood-300">
                    {error}
                </p>
            )}
        </div>
    );
}
