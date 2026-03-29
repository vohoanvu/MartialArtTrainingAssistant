interface RationaleCardProps {
  title?: string
  children: React.ReactNode
}

export function RationaleCard({ title = 'Rationale', children }: RationaleCardProps) {
  return (
    <div className="bg-parchment-200 border border-[rgba(60,50,40,0.18)] rounded-xl px-6 py-5 relative overflow-hidden">
      {/* Left border accent — the "scroll spine" */}
      <div className="absolute top-0 left-0 bottom-0 w-1.5 bg-gradient-to-b from-parchment-400 to-parchment-300 rounded-l-xl" />

      <div className="pl-2">
        <h3 className="font-serif text-lg font-semibold text-slate-zen600 mb-2">
          {title}
        </h3>
        <div className="font-sans text-sm text-slate-zen500 leading-relaxed">
          {children}
        </div>
      </div>
    </div>
  )
}
