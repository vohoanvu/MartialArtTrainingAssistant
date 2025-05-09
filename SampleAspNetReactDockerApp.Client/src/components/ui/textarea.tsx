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
                    //'focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500',
                    'text-gray-700 placeholder-gray-400',
                    'disabled:bg-gray-100 disabled:cursor-not-allowed',
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