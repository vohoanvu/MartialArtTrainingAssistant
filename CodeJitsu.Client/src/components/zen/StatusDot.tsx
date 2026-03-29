type Status = 'completed' | 'active' | 'pending' | 'cancelled'

const statusConfig: Record<Status, { color: string; label: string }> = {
  completed: { color: '#2D7A4F', label: 'Completed' },
  active:    { color: '#2D5FA0', label: 'Active' },
  pending:   { color: '#C4920A', label: 'Pending' },
  cancelled: { color: '#C45A47', label: 'Cancelled' },
}

export function StatusDot({ status }: { status: Status }) {
  const { color, label } = statusConfig[status]
  return (
    <span className="inline-flex items-center gap-1.5 font-sans text-sm text-slate-zen400">
      <span
        className="w-2 h-2 rounded-full flex-shrink-0"
        style={{ background: color }}
      />
      {label}
    </span>
  )
}
