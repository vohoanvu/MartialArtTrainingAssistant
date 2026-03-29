interface ZenDividerProps {
  label?: string
}

export function ZenDivider({ label }: ZenDividerProps) {
  return (
    <div className="flex items-center gap-4 my-6 text-slate-zen300">
      <div className="flex-1 h-px bg-gradient-to-r from-transparent via-parchment-300 to-transparent" />
      {label && (
        <span className="font-display text-label uppercase tracking-[0.1em] text-slate-zen300 whitespace-nowrap">
          {label}
        </span>
      )}
      <div className="flex-1 h-px bg-gradient-to-r from-transparent via-parchment-300 to-transparent" />
    </div>
  )
}
