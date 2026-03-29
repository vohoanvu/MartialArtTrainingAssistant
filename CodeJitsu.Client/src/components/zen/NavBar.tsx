import { cn } from '@/lib/utils'
import { Link, useLocation } from 'react-router-dom'

interface NavLink {
  label: string
  href: string
}

interface NavBarProps {
  links: NavLink[]
  userEmail?: string
  onLogout?: () => void
  rightSlot?: React.ReactNode
}

export function NavBar({ links, userEmail, onLogout, rightSlot }: NavBarProps) {
  const location = useLocation()

  return (
    <nav className="bg-slate-zen700 border-b border-white/5 sticky top-0 z-50">
      <div className="max-w-[1120px] mx-auto px-5 flex items-center justify-between h-14">
        {/* Brand */}
        <Link to="/" className="flex items-center gap-2">
          <img src="/codejitsu-favicon.png" alt="CodeJitsu" className="w-7 h-7" />
          <span className="font-display text-base font-semibold text-parchment-100 tracking-wider">
            CodeJitsu
          </span>
        </Link>

        {/* Links */}
        <div className="flex items-center gap-6">
          {links.map((link) => {
            const isActive = location.pathname === link.href
            return (
              <Link
                key={link.label}
                to={link.href}
                className={cn(
                  'font-sans text-sm transition-colors duration-fast ease-zen',
                  isActive
                    ? 'text-parchment-100 border-b border-samurai-300 pb-0.5'
                    : 'text-parchment-300 hover:text-parchment-100'
                )}
              >
                {link.label}
              </Link>
            )
          })}

          {userEmail && (
            <span className="hidden md:block font-sans text-sm text-parchment-300">
              Welcome <strong className="text-parchment-100 font-medium">{userEmail}</strong>
            </span>
          )}

          {onLogout && (
            <button
              onClick={onLogout}
              className="font-sans text-sm text-parchment-200 border border-parchment-300/30 rounded-md px-3 py-1.5 hover:bg-parchment-300/10 hover:text-parchment-100 transition-all duration-fast ease-zen"
            >
              Logout
            </button>
          )}

          {rightSlot}
        </div>
      </div>
    </nav>
  )
}
