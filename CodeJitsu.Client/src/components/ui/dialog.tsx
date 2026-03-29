import * as React from 'react';
import { cn } from '@/lib/utils';
import { X } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface DialogProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    children: React.ReactNode;
}

const Dialog: React.FC<DialogProps> = ({ open, onOpenChange, children }) => {
    if (!open) return null;

    return (
        <div className="fixed inset-0 z-50 bg-ink-400/50 flex items-center justify-center p-4">
            <div className="relative w-full max-w-5xl bg-parchment-50 border border-[rgba(60,50,40,0.18)] rounded-xl shadow-zen-lg">
                <Button
                    variant="ghost"
                    size="icon"
                    className="absolute top-3 right-3"
                    onClick={() => onOpenChange(false)}
                >
                    <X className="h-4 w-4" />
                </Button>
                {children}
            </div>
        </div>
    );
};

const DialogContent: React.FC<{ children: React.ReactNode; className?: string }> = ({
    children,
    className,
}) => (
    <div className={cn('p-6', className)}>{children}</div>
);

const DialogHeader: React.FC<{ children: React.ReactNode; className?: string }> = ({
    children,
    className,
}) => (
    <div className={cn('mb-4', className)}>{children}</div>
);

const DialogTitle: React.FC<{ children: React.ReactNode; className?: string }> = ({
    children,
    className,
}) => (
    <h2 className={cn('font-serif text-xl font-semibold text-ink-400', className)}>{children}</h2>
);

const DialogDescription: React.FC<{ children: React.ReactNode; className?: string }> = ({
    children,
    className,
}) => (
    <p className={cn('font-sans text-sm text-slate-zen400', className)}>{children}</p>
);

export { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription };
