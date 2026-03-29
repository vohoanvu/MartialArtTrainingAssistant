import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "@/lib/utils"

const badgeVariants = cva(
  'inline-flex items-center px-2.5 py-0.5 rounded-full font-display text-label font-semibold uppercase tracking-[0.08em]',
  {
    variants: {
      variant: {
        blue:   'bg-samurai-100 text-samurai-500',
        parch:  'bg-parchment-200 text-slate-zen500',
        slate:  'bg-slate-zen700 text-parchment-100',
        red:    'bg-blood-100 text-blood-400',
        gold:   'bg-gold-bg text-gold-text',
      },
    },
    defaultVariants: { variant: 'blue' },
  }
)

export interface BadgeProps
  extends React.HTMLAttributes<HTMLSpanElement>,
    VariantProps<typeof badgeVariants> {}

const Badge = React.forwardRef<HTMLSpanElement, BadgeProps>(
  ({ className, variant, ...props }, ref) => (
    <span
      ref={ref}
      className={cn(badgeVariants({ variant, className }))}
      {...props}
    />
  )
)
Badge.displayName = "Badge"

// eslint-disable-next-line react-refresh/only-export-components
export { Badge, badgeVariants }
