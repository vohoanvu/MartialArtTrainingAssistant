import * as React from "react"
import { Slot } from "@radix-ui/react-slot"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "@/lib/utils"

const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 font-sans text-sm font-medium rounded-md border transition-all duration-fast ease-zen cursor-pointer whitespace-nowrap disabled:opacity-50 disabled:pointer-events-none',
  {
    variants: {
      variant: {
        primary: [
          'bg-samurai-400 text-white border-samurai-500',
          'hover:bg-samurai-500 hover:shadow-zen-md hover:-translate-y-px',
        ],
        dark: [
          'bg-slate-zen700 text-parchment-100 border-slate-zen800',
          'font-display tracking-[0.05em]',
          'hover:bg-slate-zen800',
        ],
        cta: [
          'bg-blood-300 text-white border-blood-400',
          'font-serif italic font-semibold text-base',
          'hover:bg-blood-400 hover:-translate-y-px hover:shadow-zen-md',
          'shadow-[0_2px_12px_rgba(196,90,71,0.30)]',
        ],
        secondary: [
          'bg-parchment-100 text-ink-400 border-[rgba(60,50,40,0.18)]',
          'hover:bg-parchment-200',
        ],
        ghost: [
          'bg-transparent text-slate-zen400 border-[rgba(60,50,40,0.10)]',
          'hover:bg-parchment-100 hover:text-ink-400',
        ],
        danger: [
          'bg-blood-400 text-white border-blood-500',
          'hover:bg-blood-500',
        ],
      },
      size: {
        sm:   'h-8  px-3  text-xs',
        md:   'h-10 px-5  text-sm',
        lg:   'h-12 px-8  text-base',
        icon: 'h-10 w-10',
        full: 'w-full h-12 px-8 text-base',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'md',
    },
  }
)

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : "button"
    return (
      <Comp
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        {...props}
      />
    )
  }
)
Button.displayName = "Button"

// eslint-disable-next-line react-refresh/only-export-components
export { Button, buttonVariants }
