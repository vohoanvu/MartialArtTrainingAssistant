import { TextareaHTMLAttributes, forwardRef } from 'react';
import { cn } from '@/lib/utils';

export interface TextareaProps
    extends TextareaHTMLAttributes<HTMLTextAreaElement> {
    className?: string;
}

const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
    ({ className, ...props }, ref) => {
        return (
            <textarea
                className={cn(
                    'w-full bg-parchment-50 border-[1.5px] border-[rgba(60,50,40,0.18)] rounded-md',
                    'px-3.5 py-2.5 font-sans text-sm text-ink-400',
                    'placeholder:text-slate-zen300',
                    'outline-none transition-all duration-fast ease-zen',
                    'focus:border-samurai-400 focus:ring-2 focus:ring-samurai-400/10',
                    'disabled:bg-parchment-200 disabled:cursor-not-allowed disabled:opacity-50',
                    'resize-y',
                    className
                )}
                ref={ref}
                {...props}
            />
        );
    }
);

Textarea.displayName = 'Textarea';

export { Textarea };
