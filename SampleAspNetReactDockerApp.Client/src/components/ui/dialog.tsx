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
        <div className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4">
            <div className="relative w-full max-w-5xl bg-background rounded-lg shadow-lg">
                <Button
                    variant="ghost"
                    className="absolute top-2 right-2"
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
    <h2 className={cn('text-lg font-semibold text-foreground', className)}>{children}</h2>
);

const DialogDescription: React.FC<{ children: React.ReactNode; className?: string }> = ({
    children,
    className,
}) => (
    <p className={cn('text-sm text-muted-foreground', className)}>{children}</p>
);

export { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription };