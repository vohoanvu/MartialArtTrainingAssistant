import { useEffect, useState } from 'react';
import { Input } from '@/components/ui/input';
import { cn } from '@/lib/utils';

interface NumberFieldProps {
    id?: string;
    value: number | null;
    onChange: (value: number | null) => void;
    min?: number;
    max?: number;
    step?: number;
    placeholder?: string;
    disabled?: boolean;
    invalid?: boolean;
    describedBy?: string;
    'aria-label'?: string;
    className?: string;
}

const toText = (v: number | null) => (v === null || Number.isNaN(v) ? '' : String(v));
const parse = (text: string): number | null => (text.trim() === '' ? null : Number(text));

/**
 * Numeric input that keeps the raw text locally (so "0." or an emptied field is not clobbered
 * while typing) and reports `null` for an empty field.
 */
export function NumberField({
    id,
    value,
    onChange,
    min,
    max,
    step,
    placeholder,
    disabled,
    invalid,
    describedBy,
    'aria-label': ariaLabel,
    className,
}: NumberFieldProps) {
    const [text, setText] = useState(toText(value));

    useEffect(() => {
        // Re-sync only when the external value diverges from what the user typed.
        if (parse(text) !== value) setText(toText(value));
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [value]);

    return (
        <Input
            id={id}
            type="number"
            inputMode="decimal"
            value={text}
            min={min}
            max={max}
            step={step}
            placeholder={placeholder}
            disabled={disabled}
            aria-label={ariaLabel}
            aria-invalid={invalid || undefined}
            aria-describedby={describedBy}
            className={cn(invalid && 'border-blood-300 focus:border-blood-300 focus:ring-blood-300/10', className)}
            onChange={(e) => {
                setText(e.target.value);
                onChange(parse(e.target.value));
            }}
        />
    );
}
