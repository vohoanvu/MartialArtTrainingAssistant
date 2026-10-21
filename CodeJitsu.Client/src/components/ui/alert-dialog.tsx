import * as React from 'react';
import { createPortal } from 'react-dom';
import { Button } from '@/components/ui/button';

export interface AlertDialogProps {
    open: boolean;
    title: string;
    description: string;
    confirmLabel: string;
    cancelLabel: string;
    onConfirm: () => void;
    onCancel: () => void;
    destructive?: boolean;
    busy?: boolean;
}

const FOCUSABLE = 'button:not([disabled]), [href], input:not([disabled]), select:not([disabled])';

/**
 * Modal confirmation (role="alertdialog"). Focuses the safe (cancel) action on open,
 * closes on Escape, traps Tab inside the dialog and restores focus when closed.
 */
function AlertDialog({
    open,
    title,
    description,
    confirmLabel,
    cancelLabel,
    onConfirm,
    onCancel,
    destructive = false,
    busy = false,
}: AlertDialogProps) {
    const panelRef = React.useRef<HTMLDivElement>(null);
    const cancelRef = React.useRef<HTMLButtonElement>(null);
    const titleId = React.useId();
    const descId = React.useId();

    React.useEffect(() => {
        if (!open) return;
        const previouslyFocused = document.activeElement as HTMLElement | null;
        cancelRef.current?.focus();
        return () => previouslyFocused?.focus?.();
    }, [open]);

    if (!open) return null;

    const onKeyDown = (e: React.KeyboardEvent<HTMLDivElement>) => {
        if (e.key === 'Escape' && !busy) {
            e.stopPropagation();
            onCancel();
            return;
        }
        if (e.key !== 'Tab' || !panelRef.current) return;
        const items = Array.from(panelRef.current.querySelectorAll<HTMLElement>(FOCUSABLE));
        if (items.length === 0) return;
        const first = items[0];
        const last = items[items.length - 1];
        if (e.shiftKey && document.activeElement === first) {
            e.preventDefault();
            last.focus();
        } else if (!e.shiftKey && document.activeElement === last) {
            e.preventDefault();
            first.focus();
        }
    };

    return createPortal(
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-ink-400/50 p-4" onKeyDown={onKeyDown}>
            <div
                ref={panelRef}
                role="alertdialog"
                aria-modal="true"
                aria-labelledby={titleId}
                aria-describedby={descId}
                className="w-full max-w-md rounded-xl border border-[rgba(60,50,40,0.18)] bg-parchment-50 p-6 shadow-zen-lg"
            >
                <h2 id={titleId} className="font-serif text-lg font-semibold text-ink-400">
                    {title}
                </h2>
                <p id={descId} className="mt-2 font-sans text-sm text-slate-zen400">
                    {description}
                </p>
                <div className="mt-5 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
                    <Button ref={cancelRef} type="button" variant="secondary" onClick={onCancel} disabled={busy}>
                        {cancelLabel}
                    </Button>
                    <Button
                        type="button"
                        variant={destructive ? 'danger' : 'primary'}
                        onClick={onConfirm}
                        disabled={busy}
                    >
                        {confirmLabel}
                    </Button>
                </div>
            </div>
        </div>,
        document.body
    );
}

export { AlertDialog };
