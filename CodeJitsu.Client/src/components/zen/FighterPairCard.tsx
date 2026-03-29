import { cn } from '@/lib/utils'

interface FighterPairCardProps {
  fighter1: string
  fighter2: string
  onClick?: () => void
}

export function FighterPairCard({ fighter1, fighter2, onClick }: FighterPairCardProps) {
  return (
    <div
      onClick={onClick}
      className={cn(
        'bg-parchment-50 border border-[rgba(60,50,40,0.18)] rounded-lg',
        'px-5 py-4 flex items-center justify-between gap-3',
        'hover:border-samurai-300 hover:shadow-zen-md hover:-translate-y-px',
        'transition-all duration-base ease-zen cursor-pointer',
        'relative overflow-hidden group'
      )}
    >
      {/* Hover gradient */}
      <div className="absolute inset-0 bg-gradient-to-br from-samurai-400/[0.04] to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-base ease-zen" />

      {/* Fighter 1 */}
      <span className="font-serif text-base font-semibold text-ink-400 relative z-10">
        {fighter1}
      </span>

      {/* VS Divider */}
      <span className="font-display text-lg font-bold text-ink-300 flex-shrink-0 relative z-10 tracking-[0.04em]">
        VS
      </span>

      {/* Fighter 2 */}
      <span className="font-serif text-base font-semibold text-ink-400 relative z-10">
        {fighter2}
      </span>
    </div>
  )
}
