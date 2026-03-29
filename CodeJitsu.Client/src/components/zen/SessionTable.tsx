import { cn } from '@/lib/utils'

interface SessionTableProps {
  children: React.ReactNode
  className?: string
}

export function SessionTableWrapper({ children, className }: SessionTableProps) {
  return (
    <div className={cn(
      'bg-parchment-50 border border-[rgba(60,50,40,0.18)] rounded-xl overflow-hidden shadow-zen-sm',
      className
    )}>
      {children}
    </div>
  )
}
