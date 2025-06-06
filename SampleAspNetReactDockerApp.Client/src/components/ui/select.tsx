import { forwardRef } from 'react';
import { cn } from '@/lib/utils';
import * as SelectPrimitive from '@radix-ui/react-select';
import { ChevronDown } from 'lucide-react';

export interface SelectProps
    extends SelectPrimitive.SelectProps {
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
            'w-full px-3 py-2 border rounded-md shadow-sm',
            'flex items-center justify-between',
            'bg-background text-foreground border-input',
            'focus:outline-none focus:ring-2 focus:ring-ring focus:border-primary',
            'disabled:bg-muted disabled:cursor-not-allowed',
            className
        )}
        {...props}
    >
        {children}
        <SelectPrimitive.Icon>
            <ChevronDown className="h-4 w-4 text-muted-foreground" />
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
                'bg-popover border border-border rounded-md shadow-lg',
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
            'px-3 py-2 text-foreground hover:bg-accent hover:text-accent-foreground',
            'cursor-pointer',
            'focus:outline-none focus:bg-accent focus:text-accent-foreground',
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