interface HeroSectionProps {
  eyebrow?: string
  title: string
  subtitle: string
  ctaLabel: string
  onCta?: () => void
  children?: React.ReactNode
}

export function HeroSection({ eyebrow, title, subtitle, ctaLabel, onCta, children }: HeroSectionProps) {
  return (
    <section
      className="relative bg-slate-zen700 min-h-[80vh] flex flex-col items-center justify-center text-center px-5 py-8 overflow-hidden"
      style={{
        backgroundImage: 'url(/images/hero-background.png)',
        backgroundSize: 'cover',
        backgroundPosition: 'center',
      }}
    >
      {/* Dark overlay */}
      <div className="absolute inset-0 bg-slate-zen700/80" />

      {/* Atmospheric gradients */}
      <div className="absolute inset-0 bg-[radial-gradient(ellipse_80%_60%_at_75%_50%,rgba(45,95,160,0.18),transparent)]" />
      <div className="absolute inset-0 bg-[radial-gradient(ellipse_50%_80%_at_20%_80%,rgba(140,45,31,0.10),transparent)]" />

      {/* Bottom accent line */}
      <div className="absolute bottom-0 left-0 right-0 h-px bg-gradient-to-r from-transparent via-gold-accent/50 to-transparent" />

      {/* Content */}
      <div className="relative z-10 max-w-3xl mx-auto">
        {eyebrow && (
          <p className="font-display text-label font-semibold uppercase tracking-[0.2em] text-parchment-300 mb-4">
            {eyebrow}
          </p>
        )}
        <h1 className="font-serif text-hero font-bold text-parchment-50 leading-tight mb-3">
          {title}
        </h1>
        <p className="font-sans text-base text-parchment-300 max-w-lg mx-auto mb-6">
          {subtitle}
        </p>
        <button
          onClick={onCta}
          className="inline-block bg-blood-300 text-white font-serif italic font-semibold text-base px-8 py-3.5 rounded-lg border border-blood-400 shadow-[0_2px_12px_rgba(196,90,71,0.30)] hover:bg-blood-400 hover:-translate-y-px transition-all duration-fast ease-zen"
        >
          {ctaLabel}
        </button>
        {children}
      </div>
    </section>
  )
}
