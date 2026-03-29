# CodeJitsu — Zen Samurai Design System Specification
**Version:** 1.0  
**Status:** Authoritative — All agents must follow this document  
**Stack:** React 19 + TypeScript + Vite 7 + Shadcn (Radix UI) + Tailwind CSS v4 + Lucide React  

---

## Table of Contents

1. [Design Philosophy](#1-design-philosophy)
2. [Tech Stack & Tooling Rules](#2-tech-stack--tooling-rules)
3. [File Structure](#3-file-structure)
4. [Color System](#4-color-system)
5. [Tailwind Configuration](#5-tailwind-configuration)
6. [Typography](#6-typography)
7. [Spacing & Layout](#7-spacing--layout)
8. [Border Radius](#8-border-radius)
9. [Shadows & Elevation](#9-shadows--elevation)
10. [Motion & Transitions](#10-motion--transitions)
11. [Parchment Texture](#11-parchment-texture)
12. [Component Library](#12-component-library)
13. [Shadcn Component Overrides](#13-shadcn-component-overrides)
14. [Icon Usage](#14-icon-usage)
15. [Page-Level Patterns](#15-page-level-patterns)
16. [Screen-by-Screen Specifications](#16-screen-by-screen-specifications)
17. [Agent Rules & Constraints](#17-agent-rules--constraints)
18. [Implementation Checklist](#18-implementation-checklist)

---

## 1. Design Philosophy

The Zen Samurai aesthetic draws from three Japanese concepts that agents must internalize before writing a single line of code:

- **Ma (間)** — Negative space with purpose. Every element earns its place. Empty space is not wasted space; it is breathing room.
- **Shibui (渋い)** — Quiet, restrained beauty. Nothing is loud. Nothing competes. The UI recedes so the content leads.
- **Mono no Aware (物の哀れ)** — Poignancy of things. The interface should feel considered and human, not sterile.

### Tone Keywords
`warm` · `aged` · `intentional` · `quiet strength` · `ritual` · `focused` · `dojo`

### What This Looks Like in Practice
- Light surfaces feel like **aged rice paper**, not white plastic
- Dark surfaces feel like **deep slate** or **ink**, never pure black
- Typography mixes **architectural display** (Cinzel) with **narrative serif** (Noto Serif) and **data sans** (Noto Sans)
- Interactions are **decisive but never rushed** — 120–220ms ease-out
- The VS divider in fighter pairs, the scroll-style rationale card, the calligraphy table headers — each component should feel like a **deliberate ritual object**, not a generic UI widget

---

## 2. Tech Stack & Tooling Rules

### Stack
| Layer | Tool | Version |
|---|---|---|
| Framework | React | 19 |
| Language | TypeScript | 5.x |
| Build | Vite | 7 |
| UI primitives | Shadcn (Radix UI) | Latest |
| Styling | Tailwind CSS | v4 |
| Icons | Lucide React | Latest |
| Fonts | Google Fonts (Cinzel + Noto Serif + Noto Sans) | CDN |

### Critical Rules for Agents

**NEVER do these:**
- Hardcode any hex color, pixel value, or font name directly in a component
- Use Tailwind's default color palette (e.g. `bg-blue-500`, `text-gray-700`) — always use custom tokens
- Import icons from any library other than Lucide React
- Override Shadcn component internals with `!important`
- Use `inline style` for anything covered by a design token
- Use `font-family: Inter` or any other non-system font not listed in this spec

**ALWAYS do these:**
- Reference colors via CSS custom properties: `var(--parchment-100)`, `var(--samurai-400)`, etc.
- Use Tailwind utility classes that map to those custom properties (configured in `tailwind.config.ts`)
- Follow the component patterns in Section 12 exactly — do not invent new patterns
- When in doubt about a color, spacing, or font choice: consult this document first

---

## 3. File Structure

Agents must place files in the correct locations. Do not deviate.

```
src/
├── styles/
│   ├── tokens.css          ← CSS custom properties (single source of truth)
│   ├── globals.css         ← body reset, texture overlay, base element styles
│   └── fonts.css           ← @font-face or Google Fonts @import
├── components/
│   ├── ui/                 ← Shadcn primitives (auto-generated, then overridden)
│   │   ├── button.tsx
│   │   ├── badge.tsx
│   │   ├── input.tsx
│   │   ├── select.tsx
│   │   ├── table.tsx
│   │   └── card.tsx
│   ├── zen/                ← Custom Zen Samurai components (NOT in Shadcn)
│   │   ├── FighterPairCard.tsx
│   │   ├── RationaleCard.tsx
│   │   ├── SessionTable.tsx
│   │   ├── NavBar.tsx
│   │   ├── PageWrapper.tsx
│   │   ├── SectionHeader.tsx
│   │   ├── StatusDot.tsx
│   │   ├── ZenDivider.tsx
│   │   └── HeroSection.tsx
│   └── layout/
│       ├── AppShell.tsx
│       └── DarkSurface.tsx
├── pages/
│   ├── Landing.tsx
│   ├── Sessions.tsx
│   ├── ClassDetails.tsx
│   └── [any new page].tsx
├── hooks/
├── lib/
└── main.tsx

public/
├── assets/
│   ├── images/
│   │   └── hero-bg.webp       ← MANUALLY created by human, do not generate
│   └── logo/
│       ├── logo-dark.svg      ← MANUALLY created by human
│       └── logo-light.svg     ← MANUALLY created by human
├── favicon.svg
├── favicon-32.png
└── apple-touch-icon.png
```

> **Note for agents:** Files under `public/assets/` are created by the human team member, not by agents. Never attempt to generate, replace, or reference placeholder images. If an asset is missing, log a warning in a comment and use a CSS background color fallback.

---

## 4. Color System

### 4.1 The Five Ramps

All colors belong to one of five named ramps. Each ramp has numbered stops. Agents must use stop numbers, not approximate hex values.

#### Parchment White — Primary Background Ramp
Used for: page backgrounds, card surfaces, table backgrounds, hover states

| Stop | Hex | Usage |
|---|---|---|
| `--parchment-50` | `#FAF8F3` | Elevated card surfaces, inputs |
| `--parchment-100` | `#F2EDE0` | **Default page background** |
| `--parchment-200` | `#E8DFC8` | Table headers, section dividers, scroll cards |
| `--parchment-300` | `#D9CDAE` | Borders on parchment, muted accents |
| `--parchment-400` | `#C4B48C` | Strong borders, decorative lines |

#### Slate Gray — Dark Surfaces & Text Hierarchy
Used for: navbar, hero, dark panels, text on parchment

| Stop | Hex | Usage |
|---|---|---|
| `--slate-300` | `#9E9A93` | Muted text, placeholder text |
| `--slate-400` | `#6E6A63` | Secondary text |
| `--slate-500` | `#4A4640` | Body text (secondary) |
| `--slate-600` | `#322F2B` | Dark card backgrounds |
| `--slate-700` | `#1E1C19` | **Navbar, hero, dark surfaces** |
| `--slate-800` | `#111009` | Deepest dark, hover on dark |

#### Samurai Blue — Primary Action & Accent
Used for: primary buttons, links, focus rings, active nav items, badges

| Stop | Hex | Usage |
|---|---|---|
| `--samurai-100` | `#D4E3F5` | Badge backgrounds, info backgrounds |
| `--samurai-200` | `#A3C3E8` | Subtle accents, chart lines |
| `--samurai-300` | `#6897CC` | Hover borders on cards |
| `--samurai-400` | `#2D5FA0` | **Primary CTA buttons, links** |
| `--samurai-500` | `#1A3E72` | Button hover states |
| `--samurai-600` | `#0F2651` | Darkest blue, pressed states |

#### Blood Red — Destructive & Hero CTA Only
Used for: landing page CTA button, delete/cancel actions, error states. Use sparingly.

| Stop | Hex | Usage |
|---|---|---|
| `--blood-100` | `#F5D3CC` | Error background tints |
| `--blood-200` | `#E8998A` | Error borders |
| `--blood-300` | `#C45A47` | **Landing page CTA, delete hover** |
| `--blood-400` | `#8C2D1F` | **Destructive action buttons** |
| `--blood-500` | `#5C1812` | Pressed destructive state |

#### Ink — Primary Text
Used for: all body text on parchment backgrounds

| Stop | Hex | Usage |
|---|---|---|
| `--ink-300` | `#3A3830` | VS divider, decorative text elements |
| `--ink-400` | `#1A1916` | **Primary body text** |

#### Gold — Status & Belt Ranks
Used for: pending status, intermediate belt indicators, decorative highlights

| Token | Hex | Usage |
|---|---|---|
| `--gold-accent` | `#C4920A` | Pending status, gold belt rank, decorative divider lines |
| `--gold-bg` | `#F5EAC8` | Gold badge background |
| `--gold-text` | `#7A5A0A` | Gold badge text |

---

### 4.2 Semantic Aliases

These are the tokens agents should use in components. They abstract away the raw ramp values so future theme changes only touch `tokens.css`.

```css
/* Backgrounds */
--bg-page:        var(--parchment-100);   /* Default page bg */
--bg-surface:     var(--parchment-50);    /* Cards, inputs, elevated */
--bg-card:        #FFFFFF;                /* Pure white cards (rare) */
--bg-dark:        var(--slate-700);       /* Navbar, hero, dark panels */
--bg-dark-card:   var(--slate-600);       /* Cards on dark surfaces */

/* Text */
--text-primary:        var(--ink-400);         /* All body text on parchment */
--text-secondary:      var(--slate-400);       /* Captions, metadata */
--text-muted:          var(--slate-300);       /* Placeholders, disabled */
--text-on-dark:        var(--parchment-100);   /* Any text on dark surfaces */
--text-on-dark-muted:  var(--parchment-300);   /* Secondary text on dark */

/* Borders */
--border-light:   rgba(60, 50, 40, 0.10);  /* Default card borders */
--border-medium:  rgba(60, 50, 40, 0.18);  /* Input borders, table rows */
--border-strong:  rgba(60, 50, 40, 0.30);  /* Focused inputs, emphasis */

/* Accents */
--accent-blue:    var(--samurai-400);   /* Primary action */
--accent-red:     var(--blood-300);     /* Hero CTA */
--accent-gold:    var(--gold-accent);   /* Status, ranks */
```

---

### 4.3 Color Usage Rules

| Situation | Token to Use |
|---|---|
| Page background | `--bg-page` |
| Card / panel background | `--bg-surface` |
| Navbar / hero background | `--bg-dark` |
| Primary button background | `--samurai-400` |
| Primary button hover | `--samurai-500` |
| Hero / landing CTA button | `--blood-300` |
| Delete / destructive button | `--blood-400` |
| Body text on light bg | `--text-primary` |
| Secondary text on light bg | `--text-secondary` |
| Any text on dark bg | `--text-on-dark` |
| Table header background | `--parchment-200` |
| Table row hover | `--parchment-100` |
| Fighter card border (hover) | `--samurai-300` |
| Status — Completed | `#2D7A4F` (hardcoded, semantic green) |
| Status — Active | `--samurai-400` |
| Status — Pending | `--gold-accent` |
| Status — Cancelled | `--blood-300` |

---

## 5. Tailwind Configuration

Agents must configure `tailwind.config.ts` to expose all design tokens as Tailwind utilities. This allows using `bg-parchment-100`, `text-samurai-400`, etc. in className strings.

### `tailwind.config.ts`

```typescript
import type { Config } from 'tailwindcss'

const config: Config = {
  darkMode: 'class',
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
          // Extend, not replace — keeps default Tailwind slate AND adds zen-slate
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
    },
  },
  plugins: [],
}

export default config
```

---

## 6. Typography

### 6.1 The Three-Font System

Each font has a strict, non-overlapping role. Do not mix roles.

| Font | CSS Variable | Tailwind Class | Role |
|---|---|---|---|
| **Cinzel** | `var(--font-display)` | `font-display` | Navigation, labels, structural chrome, table headers, section eyebrows, the "VS" divider, button text on dark CTAs |
| **Noto Serif** | `var(--font-serif)` | `font-serif` | Page titles (h1), hero headings, section titles, rationale card titles, body narrative, CTA button text on hero |
| **Noto Sans** | `var(--font-sans)` | `font-sans` | Data in tables, input fields, badge text, body text for UI copy, metadata, dates |

### 6.2 Google Fonts Import

Place this in `index.html` inside `<head>`, before any CSS:

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link 
  href="https://fonts.googleapis.com/css2?family=Cinzel:wght@400;600;700&family=Noto+Serif:ital,wght@0,400;0,600;0,700;1,400&family=Noto+Sans:wght@300;400;500;600&display=swap" 
  rel="stylesheet"
>
```

### 6.3 Type Scale

| Role | Font | Size | Weight | Letter Spacing | Additional |
|---|---|---|---|---|---|
| `display` | Cinzel | `clamp(32px, 5vw, 56px)` | 700 | `0.02em` | Nav brand, cover titles |
| `h1` | Noto Serif | `clamp(24px, 4vw, 40px)` | 700 | — | Page hero headings |
| `h2` | Noto Serif | `24px` | 600 | — | Section headings |
| `h3` | Noto Serif | `18px` | 600 | — | Card titles, panel headers |
| `body` | Noto Sans | `15px` | 400 | — | `line-height: 1.7` |
| `small` | Noto Sans | `13px` | 400 | — | Metadata, captions |
| `label` | Cinzel | `11px` | 600 | `0.14em` | All uppercase structural labels |
| `mono` | Mono | `13px` | 400 | — | Dates, IDs, code |

### 6.4 Typography Rules

- **Labels always uppercase.** Any Cinzel text at 11–12px must be `text-transform: uppercase` and `letter-spacing: 0.12em` minimum.
- **Italic Noto Serif** is used for hero subheadings and CTA button text on the landing page only.
- **Never use font-weight 600+ in Noto Sans.** It looks too heavy. Use 500 for emphasis in sans contexts.
- **Page titles on dark surfaces** use `var(--text-on-dark)` (Parchment 100), never white.

---

## 7. Spacing & Layout

### 7.1 Spacing Scale

Tailwind spacing is overridden in `tailwind.config.ts`. Always use these steps:

| Token | Value | Common Usage |
|---|---|---|
| `space-1` / `p-1` | 4px | Icon padding, tiny gaps |
| `space-2` / `p-2` | 8px | Badge padding, tight gaps |
| `space-3` / `p-3` | 12px | Table cell padding, small gaps |
| `space-4` / `p-4` | 16px | Card internal padding, form gaps |
| `space-5` / `p-5` | 24px | Section inner padding, component gaps |
| `space-6` / `p-6` | 32px | Section gaps, large component spacing |
| `space-7` / `p-7` | 48px | Section vertical padding |
| `space-8` / `p-8` | 64px | Hero vertical padding, page section breaks |

### 7.2 Layout Rules

- **Max content width:** `1120px` — apply via `max-w-[1120px] mx-auto px-5`
- **Page horizontal padding:** `24px` (`px-5`) on mobile, `32px` (`px-6`) on desktop
- **Section vertical rhythm:** `py-7` (48px) minimum between major page sections, `py-8` (64px) for hero/cover sections
- **Grid system:** Use CSS Grid for all multi-column layouts. Prefer `grid-cols-2`, `grid-cols-3`, `grid-cols-4` with `gap-4` or `gap-5`. Always use `auto-fit` or explicit responsive variants — never use fixed pixel columns.

### 7.3 Responsive Breakpoints (Tailwind defaults)

| Prefix | Min Width | Notes |
|---|---|---|
| (none) | 0px | Mobile first |
| `sm:` | 640px | Large phones |
| `md:` | 768px | Tablets — most grid collapses happen here |
| `lg:` | 1024px | Desktop |
| `xl:` | 1280px | Wide desktop |

**Grid collapse rules:**
- `grid-cols-3` → `md:grid-cols-2` → `grid-cols-1` on mobile
- `grid-cols-2` → `grid-cols-1` on mobile (`md:grid-cols-2`)
- Fighter pair grid: `grid-cols-1 sm:grid-cols-2 lg:grid-cols-3`

---

## 8. Border Radius

| Token | Value | Usage |
|---|---|---|
| `rounded-sm` | 4px | Action icon buttons, small tags |
| `rounded-md` | 8px | Buttons, inputs, form controls, badges |
| `rounded-lg` | 12px | Fighter pair cards, feature cards |
| `rounded-xl` | 16px | Main panels, session table wrapper, TOC |
| `rounded-2xl` | 24px | Hero section, large modal containers |
| `rounded-full` | 999px | Status badges, avatar circles, pill tags |

---

## 9. Shadows & Elevation

| Token | Value | When to Apply |
|---|---|---|
| `shadow-zen-sm` | `0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04)` | Default card resting state |
| `shadow-zen-md` | `0 4px 16px rgba(0,0,0,0.08), 0 1px 4px rgba(0,0,0,0.06)` | Card hover, fighter pair hover, dropdowns |
| `shadow-zen-lg` | `0 8px 32px rgba(0,0,0,0.10), 0 2px 8px rgba(0,0,0,0.06)` | Modals, hero video frame |
| No shadow | — | Table rows, nav items, badges |

**Rules:**
- Never use Tailwind's default `shadow-*` utilities — always use `shadow-zen-*`
- The hero video preview frame uses `shadow-zen-lg`
- Dark surfaces (navbar, hero) never have shadows — they use background color contrast instead

---

## 10. Motion & Transitions

### 10.1 Transition Tokens

```css
--ease-zen:          cubic-bezier(0.22, 1, 0.36, 1);
--transition-fast:   120ms var(--ease-zen);   /* hover color changes */
--transition-base:   220ms var(--ease-zen);   /* card lifts, panel reveals */
--transition-slow:   400ms var(--ease-zen);   /* page transitions, overlays */
```

In Tailwind: `duration-fast`, `duration-base`, `duration-slow` with `ease-zen`.

### 10.2 Standard Interaction Patterns

| Element | Hover Effect | Transition |
|---|---|---|
| Primary button | `bg-samurai-500`, `shadow-zen-md`, `translateY(-1px)` | `fast` |
| Ghost/secondary button | `bg-parchment-200` | `fast` |
| Fighter pair card | `border-samurai-300`, `shadow-zen-md`, `translateY(-1px)` | `base` |
| Session table row | `bg-parchment-100` | `fast` |
| Action icon button | `bg-parchment-200`, `border-medium` | `fast` |
| Nav link | `text-parchment-100` (from muted) | `fast` |
| Hero CTA button | `bg-blood-400`, `translateY(-1px)` | `fast` |

### 10.3 Page Load Animation

Apply a `fadeUp` animation to primary content blocks on mount:

```css
@keyframes fadeUp {
  from { opacity: 0; transform: translateY(12px); }
  to   { opacity: 1; transform: translateY(0); }
}
```

Use staggered `animation-delay` on sibling items (e.g. fighter cards): `0ms`, `60ms`, `120ms`, `180ms`.

**Rule:** Wrap all animations in `@media (prefers-reduced-motion: no-preference)` — never animate unconditionally.

---

## 11. Parchment Texture

### What It Is

A grain/noise overlay that makes flat parchment backgrounds look like aged rice paper. It is purely visual — generated by the browser using an SVG filter, no image file required.

### Where to Apply It

In `src/styles/globals.css`:

```css
body::before {
  content: '';
  position: fixed;
  inset: 0;
  pointer-events: none;
  z-index: 9999;
  opacity: 1;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='200' height='200'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.65' numOctaves='3' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='200' height='200' filter='url(%23n)' opacity='0.04'/%3E%3C/svg%3E");
}
```

### Key Parameters

| Parameter | Value | Effect of Changing |
|---|---|---|
| `baseFrequency` | `0.65` | Higher = finer grain. Range: 0.4 (coarse) → 0.9 (very fine) |
| `numOctaves` | `3` | Higher = more detail layers. `3` is enough |
| `opacity` on `<rect>` | `0.04` | Keep between `0.03–0.06`. Higher looks dirty |

### Rules
- Only apply on `body::before`, never on individual components
- Dark surfaces (navbar, hero) are excluded automatically since the texture is multiplicative and near-invisible on dark
- Do not change this without explicit instruction — the effect is intentionally subtle

---

## 12. Component Library

This section defines every reusable component in the design system. For each component, agents receive: the visual description, the TypeScript interface, the className breakdown, and the complete implementation.

---

### 12.1 NavBar

**Description:** Dark slate background, Cinzel brand name, nav links in Noto Sans at 14px. On dark bg, text is Parchment 300, active link is Parchment 100 with a bottom border in Samurai 300.

```tsx
// src/components/zen/NavBar.tsx
import { cn } from '@/lib/utils'

interface NavBarProps {
  links: { label: string; href: string; active?: boolean }[]
  userEmail?: string
  onLogout?: () => void
}

export function NavBar({ links, userEmail, onLogout }: NavBarProps) {
  return (
    <nav className="bg-slate-zen700 border-b border-white/5">
      <div className="max-w-[1120px] mx-auto px-5 flex items-center justify-between h-14">
        
        {/* Brand */}
        <div className="flex items-center gap-2">
          <img src="/assets/logo/logo-light.svg" alt="CodeJitsu" className="w-7 h-7" />
          <span className="font-display text-base font-semibold text-parchment-100 tracking-wider">
            CodeJitsu
          </span>
        </div>

        {/* Links */}
        <div className="flex items-center gap-6">
          {links.map((link) => (
            <a
              key={link.label}
              href={link.href}
              className={cn(
                'font-sans text-sm transition-colors duration-fast ease-zen',
                link.active
                  ? 'text-parchment-100 border-b border-samurai-300 pb-0.5'
                  : 'text-parchment-300 hover:text-parchment-100'
              )}
            >
              {link.label}
            </a>
          ))}

          {userEmail && (
            <span className="font-sans text-sm text-parchment-300">
              Welcome <strong className="text-parchment-100 font-medium">{userEmail}</strong>
            </span>
          )}

          {onLogout && (
            <button
              onClick={onLogout}
              className="font-sans text-sm text-parchment-200 border border-parchment-300 rounded-md px-3 py-1.5 hover:bg-parchment-300 hover:text-slate-zen700 transition-all duration-fast ease-zen"
            >
              Logout
            </button>
          )}
        </div>
      </div>
    </nav>
  )
}
```

---

### 12.2 PageWrapper

**Description:** Sets the parchment background, max-width, and horizontal padding for all light-surface pages.

```tsx
// src/components/zen/PageWrapper.tsx
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
```

---

### 12.3 SectionHeader

**Description:** Three-layer header: eyebrow label (Cinzel uppercase), section title (Noto Serif), optional description (Noto Sans).

```tsx
// src/components/zen/SectionHeader.tsx
interface SectionHeaderProps {
  eyebrow?: string     // e.g. "01 — Foundations"
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
```

---

### 12.4 ZenDivider

**Description:** Horizontal rule with centered text, parchment-colored lines. Used between sub-sections.

```tsx
// src/components/zen/ZenDivider.tsx
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
```

---

### 12.5 Button Variants

All buttons extend Shadcn's `Button` component via `cva` variants. Override `src/components/ui/button.tsx`:

```tsx
// src/components/ui/button.tsx — Zen Samurai overrides
import { cva } from 'class-variance-authority'

const buttonVariants = cva(
  // Base classes applied to ALL buttons
  'inline-flex items-center justify-center gap-2 font-sans text-sm font-medium rounded-md border transition-all duration-fast ease-zen cursor-pointer whitespace-nowrap disabled:opacity-50 disabled:pointer-events-none',
  {
    variants: {
      variant: {
        // Primary: Samurai Blue — general primary actions
        primary: [
          'bg-samurai-400 text-white border-samurai-500',
          'hover:bg-samurai-500 hover:shadow-zen-md hover:-translate-y-px',
        ],
        // Dark: Slate — main action buttons on parchment pages (Take Attendance, Pair Up, etc.)
        dark: [
          'bg-slate-zen700 text-parchment-100 border-slate-zen800',
          'font-display tracking-[0.05em]',
          'hover:bg-slate-zen800',
        ],
        // CTA: Blood Red — landing page hero button ONLY
        cta: [
          'bg-blood-300 text-white border-blood-400',
          'font-serif italic font-semibold text-base',
          'hover:bg-blood-400 hover:-translate-y-px hover:shadow-zen-md',
          'shadow-[0_2px_12px_rgba(196,90,71,0.30)]',
        ],
        // Secondary: Parchment — less prominent actions
        secondary: [
          'bg-parchment-100 text-ink-400 border-[rgba(60,50,40,0.18)]',
          'hover:bg-parchment-200',
        ],
        // Ghost: Transparent — tertiary actions
        ghost: [
          'bg-transparent text-slate-zen400 border-[rgba(60,50,40,0.10)]',
          'hover:bg-parchment-100 hover:text-ink-400',
        ],
        // Danger: Blood Red — delete, cancel, remove
        danger: [
          'bg-blood-400 text-white border-blood-500',
          'hover:bg-blood-500',
        ],
      },
      size: {
        sm:   'h-8  px-3  text-xs',
        md:   'h-10 px-5  text-sm',     // default
        lg:   'h-12 px-8  text-base',
        full: 'w-full h-12 px-8 text-base',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'md',
    },
  }
)
```

**Usage examples:**
```tsx
<Button variant="dark" size="md">Take Attendance</Button>
<Button variant="dark" size="md">Pair Up</Button>
<Button variant="dark" size="md">Generate Today's Lessons</Button>
<Button variant="cta" size="full">Join the AI Revolution — Get Beta Access!</Button>
<Button variant="danger" size="md">Delete Session</Button>
<Button variant="primary" size="md">Save Changes</Button>
```

---

### 12.6 Badge

Override Shadcn Badge. Used for Level indicators (Kids, Beginner, etc.) and any categorical labels.

```tsx
// className patterns for each badge type
// Usage: <Badge variant="blue">Beginner</Badge>

const badgeVariants = cva(
  'inline-flex items-center px-2.5 py-0.5 rounded-full font-display text-label font-semibold uppercase tracking-[0.08em]',
  {
    variants: {
      variant: {
        blue:   'bg-samurai-100 text-samurai-500',
        parch:  'bg-parchment-200 text-slate-zen500',
        slate:  'bg-slate-zen700 text-parchment-100',
        red:    'bg-blood-100 text-blood-400',
        gold:   'bg-gold-bg text-gold-text',
      },
    },
    defaultVariants: { variant: 'blue' },
  }
)

// Level → Badge variant mapping:
// Kids       → parch
// Beginner   → blue
// Intermediate → gold
// Advanced   → slate
```

---

### 12.7 StatusDot

Used for session status in tables and detail panels.

```tsx
// src/components/zen/StatusDot.tsx
type Status = 'completed' | 'active' | 'pending' | 'cancelled'

const statusConfig: Record<Status, { color: string; label: string }> = {
  completed: { color: '#2D7A4F', label: 'Completed' },
  active:    { color: '#2D5FA0', label: 'Active' },     // samurai-400
  pending:   { color: '#C4920A', label: 'Pending' },    // gold-accent
  cancelled: { color: '#C45A47', label: 'Cancelled' },  // blood-300
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
```

---

### 12.8 SessionTable

The training session management table. Wraps a Shadcn Table with Zen Samurai styles applied.

```tsx
// src/components/zen/SessionTable.tsx
// Key className decisions:

// Wrapper:
'bg-parchment-50 border border-[rgba(60,50,40,0.18)] rounded-xl overflow-hidden shadow-zen-sm'

// <thead> <tr>:
'bg-parchment-200 border-b-2 border-[rgba(60,50,40,0.18)]'

// <th>:
'font-display text-label font-semibold uppercase tracking-[0.12em] text-slate-zen400 px-4 py-3 text-left'

// <tbody> <tr>:
'border-b border-[rgba(60,50,40,0.10)] hover:bg-parchment-100 transition-colors duration-fast ease-zen last:border-b-0'

// <td>:
'font-sans text-sm text-ink-400 px-4 py-3 align-middle'

// Date cell specifically:
'font-mono text-sm text-slate-zen400'

// Description cell:
'font-sans text-sm font-medium text-ink-400'

// Actions cell — three icon buttons in a row:
'flex items-center gap-2'

// Individual action icon button:
'w-8 h-8 flex items-center justify-center rounded-sm border border-[rgba(60,50,40,0.10)] bg-parchment-50 text-slate-zen400 cursor-pointer hover:bg-parchment-200 hover:text-ink-400 hover:border-[rgba(60,50,40,0.18)] transition-all duration-fast ease-zen'
```

**Icon mapping for action buttons (using Lucide React):**
- Edit action: `<Pencil size={14} />`
- Manage/Settings action: `<Settings size={14} />`
- Notes/Log action: `<ClipboardList size={14} />`

---

### 12.9 FighterPairCard

The RPG-menu style card showing two fighters sparring.

```tsx
// src/components/zen/FighterPairCard.tsx
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
        // Base
        'bg-parchment-50 border border-[rgba(60,50,40,0.18)] rounded-lg',
        'px-5 py-4 flex items-center justify-between gap-3',
        // Hover
        'hover:border-samurai-300 hover:shadow-zen-md hover:-translate-y-px',
        // Transition
        'transition-all duration-base ease-zen cursor-pointer',
        // Gradient overlay on hover (via group)
        'relative overflow-hidden group'
      )}
    >
      {/* Hover gradient */}
      <div className="absolute inset-0 bg-gradient-to-br from-samurai-400/4 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-base ease-zen" />
      
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
```

---

### 12.10 RationaleCard

The parchment-scroll-styled card used to display AI-generated rationale text.

```tsx
// src/components/zen/RationaleCard.tsx
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
```

---

### 12.11 HeroSection

Used on the landing page only. Dark slate background with mist gradient overlays and a centered CTA.

```tsx
// src/components/zen/HeroSection.tsx
interface HeroSectionProps {
  eyebrow?: string
  title: string
  subtitle: string
  ctaLabel: string
  onCta?: () => void
  videoSrc?: string
}

export function HeroSection({ eyebrow, title, subtitle, ctaLabel, onCta }: HeroSectionProps) {
  return (
    <section
      className="relative bg-slate-zen700 min-h-[80vh] flex flex-col items-center justify-center text-center px-5 py-8 overflow-hidden"
      style={{
        // If hero-bg.webp exists, use it; otherwise fall back gracefully
        backgroundImage: 'url(/assets/images/hero-bg.webp)',
        backgroundSize: 'cover',
        backgroundPosition: 'center',
      }}
    >
      {/* Dark overlay — always present, ensures text legibility over any bg image */}
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
      </div>
    </section>
  )
}
```

---

### 12.12 ClassSessionPanel

The detail panel showing class information and student roster on the Class Details screen. Two-column layout.

```tsx
// Key className decisions for the main panel wrapper:
'bg-parchment-50 border border-[rgba(60,50,40,0.10)] rounded-xl shadow-zen-sm p-5'

// Panel heading (e.g. "Class Session Details"):
'font-serif text-lg font-semibold text-ink-400 mb-4'

// Key-value rows within the panel:
// Label: 'font-display text-label font-semibold uppercase tracking-[0.1em] text-slate-zen400'
// Value: 'font-sans text-sm text-ink-400'

// The three action buttons row (Take Attendance, Pair Up, Generate Today's Lessons):
// Use Button variant="dark" size="md", in a flex row with gap-3, full width each
'flex gap-3 mt-5'
// Each button: flex-1 so they distribute equally
```

---

### 12.13 Input & Form Fields

Override Shadcn Input via `src/components/ui/input.tsx`:

```tsx
// Base className for all inputs:
'w-full bg-parchment-50 border-[1.5px] border-[rgba(60,50,40,0.18)] rounded-md px-3.5 py-2.5 font-sans text-sm text-ink-400 placeholder:text-slate-zen300 outline-none transition-all duration-fast ease-zen focus:border-samurai-400 focus:ring-2 focus:ring-samurai-400/10'

// Label above inputs:
'font-display text-label font-semibold uppercase tracking-[0.1em] text-slate-zen400 mb-1.5 block'

// Select field — add arrow indicator:
'appearance-none bg-[url("data:image/svg+xml,...chevron...")] bg-no-repeat bg-right pr-9'
```

---

## 13. Shadcn Component Overrides

When using Shadcn components, apply Zen Samurai styles via the `className` prop or by editing the generated file in `src/components/ui/`. Never modify `node_modules`.

### `Card`
```tsx
// Replace default Card usage with these classNames:
<Card className="bg-parchment-50 border-[rgba(60,50,40,0.10)] rounded-xl shadow-zen-sm" />
<CardHeader className="pb-3" />
<CardTitle className="font-serif text-lg font-semibold text-ink-400" />
<CardContent className="font-sans text-sm text-slate-zen400" />
```

### `Table`
```tsx
// Always wrap Table in:
<div className="bg-parchment-50 border border-[rgba(60,50,40,0.18)] rounded-xl overflow-hidden shadow-zen-sm">
  <Table />
</div>
// TableHead:
<TableHead className="bg-parchment-200 border-b-2 border-[rgba(60,50,40,0.18)]" />
// TableRow:
<TableRow className="hover:bg-parchment-100 border-[rgba(60,50,40,0.10)] transition-colors duration-fast ease-zen" />
// TableHead cell:
<th className="font-display text-label uppercase tracking-[0.12em] text-slate-zen400 px-4 py-3 text-left" />
```

### `Dialog` / Modal
```tsx
<DialogContent className="bg-parchment-50 border border-[rgba(60,50,40,0.18)] rounded-xl shadow-zen-lg" />
<DialogTitle className="font-serif text-xl font-semibold text-ink-400" />
<DialogDescription className="font-sans text-sm text-slate-zen400" />
```

### `Select`
```tsx
<SelectTrigger className="bg-parchment-50 border-[rgba(60,50,40,0.18)] rounded-md font-sans text-sm text-ink-400 focus:ring-samurai-400/20" />
<SelectContent className="bg-parchment-50 border-[rgba(60,50,40,0.18)] rounded-xl shadow-zen-md" />
<SelectItem className="font-sans text-sm text-ink-400 hover:bg-parchment-100 focus:bg-parchment-100 rounded-md" />
```

### `Toast` / Sonner
```tsx
// Success toast:
'bg-parchment-50 border-[rgba(60,50,40,0.18)] text-ink-400 font-sans'
// Error toast:
'bg-blood-100 border-blood-200 text-blood-400 font-sans'
```

---

## 14. Icon Usage

**Library:** Lucide React (already in the stack)  
**Style:** Stroke-based, 1.5px stroke width (Lucide default), never filled  
**Size:** `size={14}` for action icons inside buttons, `size={16}` for inline icons, `size={20}` for standalone icons  
**Color:** Always via `className="text-slate-zen400"` (or contextual color), never hardcoded  

### Standard Icon Mapping

| UI Element | Lucide Icon | Size |
|---|---|---|
| Edit session | `<Pencil />` | 14 |
| Manage / Settings | `<Settings />` | 14 |
| Notes / Log | `<ClipboardList />` | 14 |
| Student Roster | `<Users />` | 16 |
| Calendar / Date | `<Calendar />` | 16 |
| Duration / Time | `<Clock />` | 16 |
| Pair Up / Lightning | `<Zap />` | 16 |
| Rationale / Scroll | `<BookOpen />` | 16 |
| Generate / AI | `<Sparkles />` | 16 |
| Attendance | `<CheckSquare />` | 16 |
| Create / Add | `<Plus />` | 16 |
| Delete | `<Trash2 />` | 14 |
| Logout | `<LogOut />` | 14 |
| Dark mode toggle | `<Sun />` / `<Moon />` | 18 |

### Rules
- Never use emoji as icons in production components
- Always set explicit `size` — never rely on CSS font-size inheritance
- In dark contexts (NavBar), apply `className="text-parchment-300"`
- In action icon buttons, apply `className="text-slate-zen400"` — the parent button handles hover color

---

## 15. Page-Level Patterns

Every page in the project follows one of two base templates:

### 15.1 Light Page Template (most pages)

```tsx
// Pattern for: Sessions, ClassDetails, VideoAnalysis, Settings, any admin page
export default function SomePage() {
  return (
    <>
      <NavBar links={[...]} userEmail="..." onLogout={...} />
      <PageWrapper>
        <SectionHeader
          eyebrow="01 — Section Name"
          title="Page Title Here"
          description="Optional subtitle copy."
        />
        {/* Page content */}
      </PageWrapper>
    </>
  )
}
// Background: parchment-100 (from PageWrapper)
// Content max-width: 1120px (from PageWrapper)
```

### 15.2 Dark/Hero Page Template

```tsx
// Pattern for: Landing page ONLY
export default function Landing() {
  return (
    <>
      <NavBar links={[...]} />
      <HeroSection
        eyebrow="BJJ Dojo Management · AI-Powered"
        title="Revolutionize Your BJJ Dojo with AI Assistance"
        subtitle="..."
        ctaLabel="Join the AI Revolution — Get Beta Access!"
      />
      {/* Feature sections below use PageWrapper / light surfaces */}
      <PageWrapper>
        {/* ... */}
      </PageWrapper>
    </>
  )
}
```

### 15.3 Section Vertical Rhythm

```
NavBar (56px tall, sticky top-0 z-50)
└── Page content starts here
    ├── SectionHeader (mb-6)
    ├── Content block
    ├── ZenDivider (my-6) ← between sub-sections, optional
    ├── Content block
    └── ...
```

Each major content section: `py-7` (48px) vertical padding.  
The page's first content block (below SectionHeader): no top padding — SectionHeader handles it.

---

## 16. Screen-by-Screen Specifications

These are the three screens from the design images. All other screens in the project should follow the same patterns by extension.

---

### 16.1 Landing Page (`/`)

| Element | Spec |
|---|---|
| Background | `hero-bg.webp` + `bg-slate-zen700/80` overlay + two radial gradients |
| Heading font | Noto Serif, `text-hero`, weight 700, `text-parchment-50` |
| Subtitle font | Noto Sans, 15px, `text-parchment-300` |
| CTA button | `variant="cta"`, Blood Red, Noto Serif italic |
| Nav | Dark, logo-light.svg, Cinzel brand |
| Video preview frame | `bg-slate-zen600`, `rounded-xl`, `shadow-zen-lg`, play icon centered |
| Bottom accent | 1px gradient line `from-transparent via-gold-accent/50 to-transparent` |

---

### 16.2 Session Management (`/class-session`)

| Element | Spec |
|---|---|
| Page background | `bg-parchment-100` |
| Page title | Noto Serif, h1, underlined with a calligraphy-style border-bottom |
| Table wrapper | `bg-parchment-50`, `border border-[rgba(60,50,40,0.18)]`, `rounded-xl`, `shadow-zen-sm` |
| Table header row | `bg-parchment-200` |
| Column headers | Cinzel, `text-label`, uppercase, `tracking-[0.12em]`, `text-slate-zen400` |
| Date cells | Mono font, `text-slate-zen400` |
| Description cells | Noto Sans, medium weight, `text-ink-400` |
| Status | `<StatusDot />` component |
| Level | `<Badge />` component |
| Action icons | `<Pencil />`, `<Settings />`, `<ClipboardList />` — each in icon button wrapper |
| Create button | `variant="dark"`, centered below table, `px-8 py-3` |

---

### 16.3 Class Details & Pairing (`/class-session/:id`)

| Element | Spec |
|---|---|
| Page title | Noto Serif h1, centered, `text-ink-400` |
| Main panel | `bg-parchment-50`, `rounded-xl`, `border border-[rgba(60,50,40,0.10)]`, `shadow-zen-sm` |
| Panel layout | Two-column: left = session details, right = students roster |
| Session detail labels | Cinzel labels, Noto Sans values |
| Action buttons | Three `variant="dark"` buttons in a row: Take Attendance, Pair Up, Generate Today's Lessons |
| Fighter Pairs heading | Noto Serif h2, `text-ink-400`, `mb-4` |
| Fighter pair grid | `grid-cols-1 sm:grid-cols-2 lg:grid-cols-3`, `gap-4` |
| Fighter pair cards | `<FighterPairCard />` component |
| Rationale card | `<RationaleCard />` component, below the pair grid |

---

### 16.4 New/Unknown Pages — Decision Rules

When building a page not listed above, agents must apply this decision tree:

**Q1: Is this a public-facing marketing page?**
- Yes → Use dark hero template. Start with `<HeroSection />`. Below the fold, switch to light `<PageWrapper />`.

**Q2: Is this an authenticated admin/management page?**
- Yes → Use light page template. `<NavBar />` + `<PageWrapper />`. Start with `<SectionHeader />`.

**Q3: Does this page have a data table?**
- Yes → Use `<SessionTable />` pattern. Wrapper: `bg-parchment-50 rounded-xl border border-medium shadow-zen-sm overflow-hidden`. Table headers always Cinzel.

**Q4: Does this page have cards or grid items?**
- Yes → Use `<FighterPairCard />` pattern for interactive pair-type cards. Use Shadcn `<Card />` with overrides (Section 13) for informational cards.

**Q5: Does this page need a form?**
- Yes → Input labels: Cinzel, uppercase, 11px. Input fields: `bg-parchment-50`, `border-[rgba(60,50,40,0.18)]`, `rounded-md`, focus ring `samurai-400/20`. Submit button: `variant="dark"` or `variant="primary"`.

---

## 17. Agent Rules & Constraints

### Absolute Rules (Never Break)

1. **Never hardcode colors.** Use CSS variables or Tailwind tokens. If a color you need isn't in the system, ask before inventing it.
2. **Never use Tailwind's default color palette.** No `bg-blue-500`, `text-gray-700`, `border-red-300`, etc.
3. **Never mix font roles.** Cinzel is structural, Noto Serif is narrative, Noto Sans is data. Do not use Cinzel for body text or Noto Serif for table headers.
4. **Never use default Tailwind `shadow-*`.** Use `shadow-zen-sm`, `shadow-zen-md`, `shadow-zen-lg` only.
5. **Never use default Tailwind `rounded-*` sizes.** Use the overridden scale from Section 8.
6. **Never use `!important` to force styles.** Fix the cascade instead.
7. **Never reference `public/assets/` files that don't exist.** Use CSS fallbacks.
8. **Never invent a new component pattern.** Extend existing components from Section 12.

### Decision Rules (When Uncertain)

- **Unsure about a color?** Refer to Section 4.3 (Color Usage Rules).
- **Unsure which font to use?** Refer to Section 6.1 (Three-Font System).
- **Building a new component?** Find the closest analogue in Section 12 and extend it.
- **Building a new page?** Follow the decision tree in Section 16.4.
- **Adding an icon?** Check Section 14's icon mapping table first.

### What Agents May Do Without Asking

- Add new pages following existing templates
- Add new components that extend existing patterns
- Add Lucide icons from the standard mapping
- Add new `variant` to existing `cva` definitions, following the pattern

### What Agents Must NOT Do Without Explicit Instruction

- Change any value in `tokens.css`
- Change `tailwind.config.ts`
- Add a new font
- Add a new color not in the five ramps
- Replace or delete a file in `public/assets/`
- Change the parchment texture parameters

---

## 18. Implementation Checklist

Agents working on the initial migration should complete steps in this order. Do not skip ahead.

### Phase 1 — Foundation (no component work yet)
- [ ] Add Google Fonts `<link>` tags to `index.html`
- [ ] Create `src/styles/tokens.css` with all CSS custom properties from Section 4
- [ ] Create `src/styles/globals.css` with body reset, parchment texture (Section 11), and base element styles
- [ ] Import both files in `src/main.tsx` (before any component imports)
- [ ] Update `tailwind.config.ts` with full config from Section 5
- [ ] Verify Tailwind tokens resolve correctly by testing a `bg-parchment-100` class

### Phase 2 — Shadcn Overrides
- [ ] Override `button.tsx` with Zen Samurai variants (Section 12.5)
- [ ] Override `badge.tsx` with Zen Samurai variants (Section 12.6)
- [ ] Override `card.tsx` class defaults (Section 13)
- [ ] Override `input.tsx` class defaults (Section 13)
- [ ] Override `table.tsx` class defaults (Section 13)

### Phase 3 — Zen Custom Components
- [ ] `NavBar.tsx` (Section 12.1)
- [ ] `PageWrapper.tsx` (Section 12.2)
- [ ] `SectionHeader.tsx` (Section 12.3)
- [ ] `ZenDivider.tsx` (Section 12.4)
- [ ] `StatusDot.tsx` (Section 12.7)
- [ ] `FighterPairCard.tsx` (Section 12.9)
- [ ] `RationaleCard.tsx` (Section 12.10)
- [ ] `HeroSection.tsx` (Section 12.11)

### Phase 4 — Page Migration (in this order)
- [ ] `Landing.tsx` — easiest to verify visually
- [ ] `Sessions.tsx` — table pattern
- [ ] `ClassDetails.tsx` — compound layout
- [ ] All remaining pages — apply decision rules from Section 16.4

### Phase 5 — Polish
- [ ] Add `fadeUp` animation to content blocks (Section 10.3)
- [ ] Verify staggered animation on fighter pair cards
- [ ] Verify `prefers-reduced-motion` is respected on all animations
- [ ] Responsive check: all grids collapse correctly at `md` breakpoint
- [ ] Verify hero fallback works without `hero-bg.webp` present

---

*End of Zen Samurai Design System Specification v1.0*  
*All agents must treat this document as authoritative. Changes to this document require human approval.*
