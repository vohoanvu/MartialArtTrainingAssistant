import * as React from 'react';
import { cn } from '@/lib/utils';

export interface SwitchProps
    extends Omit<React.ButtonHTMLAttributes<HTMLButtonElement>, 'onChange' | 'role'> {
    checked: boolean;
    onCheckedChange: (checked: boolean) => void;
}

/** Accessible toggle (button with role="switch"); keyboard operable via Space/Enter. */
const Switch = React.forwardRef<HTMLButtonElement, SwitchProps>(
    ({ className, checked, onCheckedChange, disabled, ...props }, ref) => (
        <button
            ref={ref}
            type="button"
            role="switch"
            aria-checked={checked}
            disabled={disabled}
            onClick={() => onCheckedChange(!checked)}
            className={cn(
                'relative inline-flex h-6 w-11 shrink-0 items-center rounded-full border border-[rgba(60,50,40,0.18)]',
                'transition-colors duration-fast ease-zen outline-none',
                'focus-visible:ring-2 focus-visible:ring-samurai-400/30',
                'disabled:cursor-not-allowed disabled:opacity-50',
                checked ? 'bg-samurai-400' : 'bg-parchment-300',
                className
            )}
            {...props}
        >
            <span
                aria-hidden="true"
                className={cn(
                    'inline-block h-4 w-4 rounded-full bg-parchment-50 shadow-zen-sm transition-transform duration-fast ease-zen',
                    checked ? 'translate-x-6' : 'translate-x-1'
                )}
            />
        </button>
    )
);
Switch.displayName = 'Switch';

export { Switch };
