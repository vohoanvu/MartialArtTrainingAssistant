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
                    'w-full px-3 py-2 border rounded-md shadow-sm',
                    'bg-background text-foreground border-input',
                    'focus:outline-none focus:ring-2 focus:ring-ring focus:border-primary',
                    'placeholder-muted-foreground',
                    'disabled:bg-muted disabled:cursor-not-allowed',
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