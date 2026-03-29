import { cn } from '@/lib/utils'

interface PageWrapperProps {
  children: React.ReactNode
  className?: string
}

export function PageWrapper({ children, className }: PageWrapperProps) {
  return (
    <main className={cn('min-h-screen bg-parchment-100', className)}>
      <div className="max-w-[1120px] mx-auto px-5 py-7">
        {children}
      </div>
    </main>
  )
}
