import { cn } from '@/lib/utils'

interface SectionHeaderProps {
  eyebrow?: string
  title: string
  description?: string
  centered?: boolean
}

export function SectionHeader({ eyebrow, title, description, centered }: SectionHeaderProps) {
  return (
    <div className={cn('mb-6', centered && 'text-center')}>
      {eyebrow && (
        <p className="font-display text-label font-semibold uppercase tracking-[0.14em] text-slate-zen300 mb-2">
          {eyebrow}
        </p>
      )}
      <h2 className="font-serif text-2xl md:text-3xl font-semibold text-ink-400 leading-snug">
        {title}
      </h2>
      {description && (
        <p className="mt-3 font-sans text-base text-slate-zen400 max-w-xl">
          {description}
        </p>
      )}
    </div>
  )
}
