import { forwardRef } from 'react';
import { cn } from '@/lib/utils';
import * as SelectPrimitive from '@radix-ui/react-select';
import { ChevronDown } from 'lucide-react';

export interface SelectProps extends SelectPrimitive.SelectProps {
    className?: string;
}

const Select = SelectPrimitive.Root;

const SelectTrigger = forwardRef<
    HTMLButtonElement,
    SelectPrimitive.SelectTriggerProps
>(({ className, children, ...props }, ref) => (
    <SelectPrimitive.Trigger
        ref={ref}
        className={cn(
            'w-full bg-parchment-50 border-[1.5px] border-[rgba(60,50,40,0.18)] rounded-md',
            'px-3.5 py-2.5 flex items-center justify-between',
            'font-sans text-sm text-ink-400',
            'outline-none transition-all duration-fast ease-zen',
            'focus:border-samurai-400 focus:ring-2 focus:ring-samurai-400/10',
            'disabled:bg-parchment-200 disabled:cursor-not-allowed disabled:opacity-50',
            className
        )}
        {...props}
    >
        {children}
        <SelectPrimitive.Icon>
            <ChevronDown className="h-4 w-4 text-slate-zen300" />
        </SelectPrimitive.Icon>
    </SelectPrimitive.Trigger>
));
SelectTrigger.displayName = 'SelectTrigger';

const SelectContent = forwardRef<
    HTMLDivElement,
    SelectPrimitive.SelectContentProps
>(({ className, children, ...props }, ref) => (
    <SelectPrimitive.Portal>
        <SelectPrimitive.Content
            ref={ref}
            className={cn(
                'bg-parchment-50 border border-[rgba(60,50,40,0.18)] rounded-xl shadow-zen-md',
                'max-h-60 overflow-y-auto',
                'z-50',
                className
            )}
            {...props}
        >
            <SelectPrimitive.Viewport className="p-1">
                {children}
            </SelectPrimitive.Viewport>
        </SelectPrimitive.Content>
    </SelectPrimitive.Portal>
));
SelectContent.displayName = 'SelectContent';

const SelectItem = forwardRef<
    HTMLDivElement,
    SelectPrimitive.SelectItemProps
>(({ className, children, ...props }, ref) => (
    <SelectPrimitive.Item
        ref={ref}
        className={cn(
            'px-3 py-2 font-sans text-sm text-ink-400 rounded-md',
            'hover:bg-parchment-100 focus:bg-parchment-100',
            'cursor-pointer outline-none',
            className
        )}
        {...props}
    >
        <SelectPrimitive.ItemText>{children}</SelectPrimitive.ItemText>
    </SelectPrimitive.Item>
));
SelectItem.displayName = 'SelectItem';

const SelectValue = SelectPrimitive.Value;

export { Select, SelectTrigger, SelectContent, SelectItem, SelectValue };
