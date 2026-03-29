/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        parchment: {
          50:  '#FAF8F3',
          100: '#F2EDE0',
          200: '#E8DFC8',
          300: '#D9CDAE',
          400: '#C4B48C',
        },
        slate: {
          zen300: '#9E9A93',
          zen400: '#6E6A63',
          zen500: '#4A4640',
          zen600: '#322F2B',
          zen700: '#1E1C19',
          zen800: '#111009',
        },
        samurai: {
          100: '#D4E3F5',
          200: '#A3C3E8',
          300: '#6897CC',
          400: '#2D5FA0',
          500: '#1A3E72',
          600: '#0F2651',
        },
        blood: {
          100: '#F5D3CC',
          200: '#E8998A',
          300: '#C45A47',
          400: '#8C2D1F',
          500: '#5C1812',
        },
        ink: {
          300: '#3A3830',
          400: '#1A1916',
        },
        gold: {
          accent: '#C4920A',
          bg:     '#F5EAC8',
          text:   '#7A5A0A',
        },
      },
      fontFamily: {
        display: ['Cinzel', 'serif'],
        serif:   ['Noto Serif', 'Georgia', 'serif'],
        sans:    ['Noto Sans', 'Helvetica Neue', 'sans-serif'],
        mono:    ['SF Mono', 'Consolas', 'monospace'],
      },
      fontSize: {
        'label': ['11px', { lineHeight: '1', letterSpacing: '0.14em' }],
        'xs':    ['12px', { lineHeight: '1.5' }],
        'sm':    ['13px', { lineHeight: '1.6' }],
        'base':  ['15px', { lineHeight: '1.7' }],
        'lg':    ['17px', { lineHeight: '1.5' }],
        'xl':    ['20px', { lineHeight: '1.4' }],
        '2xl':   ['24px', { lineHeight: '1.3' }],
        '3xl':   ['30px', { lineHeight: '1.2' }],
        '4xl':   ['36px', { lineHeight: '1.15' }],
        'hero':  ['clamp(28px, 4vw, 48px)', { lineHeight: '1.1' }],
      },
      spacing: {
        '1': '4px',
        '2': '8px',
        '3': '12px',
        '4': '16px',
        '5': '24px',
        '6': '32px',
        '7': '48px',
        '8': '64px',
      },
      borderRadius: {
        'sm':  '4px',
        'md':  '8px',
        'lg':  '12px',
        'xl':  '16px',
        '2xl': '24px',
      },
      boxShadow: {
        'zen-sm': '0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04)',
        'zen-md': '0 4px 16px rgba(0,0,0,0.08), 0 1px 4px rgba(0,0,0,0.06)',
        'zen-lg': '0 8px 32px rgba(0,0,0,0.10), 0 2px 8px rgba(0,0,0,0.06)',
      },
      transitionTimingFunction: {
        'zen': 'cubic-bezier(0.22, 1, 0.36, 1)',
      },
      transitionDuration: {
        'fast': '120ms',
        'base': '220ms',
        'slow': '400ms',
      },
      keyframes: {
        "accordion-down": {
          from: { height: "0" },
          to: { height: "var(--radix-accordion-content-height)" },
        },
        "accordion-up": {
          from: { height: "var(--radix-accordion-content-height)" },
          to: { height: "0" },
        },
        "shimmer": {
          '0%': { transform: 'translateX(-100%)' },
          '100%': { transform: 'translateX(100%)' },
        },
      },
      animation: {
        "accordion-down": "accordion-down 0.2s ease-out",
        "accordion-up": "accordion-up 0.2s ease-out",
        "shimmer": "shimmer 2s linear infinite",
      },
    },
  },
  plugins: [],
}
