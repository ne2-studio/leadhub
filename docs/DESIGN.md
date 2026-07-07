---
version: alpha
name: Exeal
description: Design system for exeal.com — developer-training brand built on a dark "terminal navy" base with a signal-green accent and an amber CTA color.
colors:
  primary: "#05e69b"
  secondary: "#e6ad1c"
  ink: "#182935"
  surface: "#ffffff"
  surface-subtle: "#f3f4f4"
  surface-tint: "#f1fff9"
  neutral-muted: "#8d9199"
  neutral-faint: "#d1d3d6"
  contrast: "#000000"
typography:
  h1:
    fontFamily: Gopher Medium
    fontSize: 1.875em
    fontWeight: 400
    lineHeight: 1.25em
  h2:
    fontFamily: Gopher Regular
    fontSize: 1.5em
    fontWeight: 400
  h3:
    fontFamily: Gopher Regular
    fontSize: 1.375em
    fontWeight: 700
  h4:
    fontFamily: Avenir Book
    fontSize: 1.375em
    fontWeight: 400
  body-md:
    fontFamily: Avenir Book
    fontSize: "{spacing.base}"
    fontWeight: 400
    lineHeight: 1.75rem
    letterSpacing: 0.02rem
  label-lg:
    fontFamily: Gopher Regular
    fontSize: 1.3em
    fontWeight: 400
  label-md:
    fontFamily: Gopher Regular
    fontSize: 1.1em
    fontWeight: 400
  code:
    fontFamily: JetBrainsMono-Regular
    fontSize: 1em
    fontWeight: 400
rounded:
  none: 0px
  sm: 5px
  lg: 2rem
  full: 9999px
spacing:
  base: 18.7px
  xs: 0.25rem
  sm: 0.5rem
  md: 1em
  lg: 1.25em
  xl: 2em
  section: 3em
components:
  button-primary:
    backgroundColor: "{colors.secondary}"
    textColor: "{colors.ink}"
    typography: "{typography.label-lg}"
    rounded: "{rounded.lg}"
    padding: 0.3em 1.4em
  button-accent:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.ink}"
    typography: "{typography.label-lg}"
    rounded: "{rounded.lg}"
    padding: 0.3em 1.4em
  button-dark:
    backgroundColor: "{colors.ink}"
    textColor: "{colors.surface-tint}"
    typography: "{typography.label-lg}"
    rounded: "{rounded.lg}"
    padding: 0.3em 1.4em
  button-hollow:
    backgroundColor: transparent
    textColor: "{colors.secondary}"
    borderColor: "{colors.secondary}"
    typography: "{typography.label-lg}"
    rounded: "{rounded.lg}"
    padding: 0.3em 1.4em
  nav-button:
    backgroundColor: "{colors.secondary}"
    textColor: "{colors.ink}"
    typography: "{typography.label-md}"
    rounded: "{rounded.lg}"
  input:
    backgroundColor: transparent
    borderColor: "{colors.primary}"
    rounded: "{rounded.none}"
    typography: "{typography.body-md}"
  tag:
    borderColor: currentColor
    rounded: "{rounded.sm}"
    typography: "{typography.body-md}"
  card:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.none}"
    padding: 1.5rem
  box:
    borderColor: "{colors.primary}"
    rounded: "{rounded.none}"
    padding: 2em 1em 1em
  price:
    typography: "{typography.h2}"
    textColor: "{colors.surface-tint}"
---

# Exeal Design System

## Overview

Exeal trains software developers — clean code, TDD, legacy rescue, architecture, high-performing teams. The brand borrows from developer culture rather than generic corporate-training visuals: a near-black **Terminal Navy** anchors most surfaces, a neon **Signal Green** marks proof/emphasis/success, and a warm **Amber** drives calls to action. Backgrounds are frequently full-bleed code-editor screenshots washed in a dark teal tint, and a monospace face (JetBrains Mono) is reserved exclusively for code blocks, keeping the "developer" signal deliberate rather than decorative.

The overall feel is confident and outcome-oriented rather than playful: bold pill-shaped CTAs, an em-based type scale for long-form readability, and one distinctive custom shape (the rounded-flap card, see **Shapes**) as the brand's signature mark. Copy is bilingual (Spanish default, English secondary) and practitioner-voiced — specific and outcome-driven, not generic corporate tone.

## Colors

The palette is rooted in a dark, near-black "terminal" neutral, with a single neon accent doing double duty as both a highlight color and a "compiled/success" signal, and a warm amber reserved for calls to action.

- **Primary — Signal Green (`#05e69b`):** The brand's signature neon/mint accent. Used for emphasis inside headings, ticked-list checkmarks, box borders, price-numeral outlines, and the "affirmative" button variant (form submit). Never used as a full-page background.
- **Secondary — Amber (`#e6ad1c`):** The default call-to-action color. Solid-fill buttons, nav CTA pills, arrow icons, and article link underlines. Green marks emphasis/proof; amber marks "click here" — the two are never used as competing CTAs in the same component.
- **Ink — Terminal Navy (`#182935`):** The dark anchor color. Background for `dark`-themed sections, default text color on light sections. This is the brand's "black" for all practical purposes; true black (`contrast`) is reserved for the footer only.
- **Surface (`#ffffff`):** Background for `light`-themed sections.
- **Surface Subtle (`#f3f4f4`):** Neutral light-gray section background, used as an alternative to pure white for visual variety between sections.
- **Surface Tint — Gloaming Mint (`#f1fff9`):** A very pale mint background, used for a softer alternative to plain white/gray (pricing panels, syllabus sections). Also doubles as the fill color for outlined price numerals.
- **Neutral Muted (`#8d9199`):** Secondary/caption text — descriptions, footer links, form labels.
- **Neutral Faint (`#d1d3d6`):** Tertiary text and list items on dark surfaces.
- **Contrast (`#000000`):** True black, used exclusively as the footer background.

## Typography

Three custom webfonts carry the whole system — no external font CDN, no system-font fallback stack beyond a generic `sans-serif`/`monospace` safety net.

- **Headlines (`h1`):** Gopher Medium at 1.875em — the display face, used sparingly (page/section headlines only). An `<em>` inside an `h1` switches to Primary green as an inline keyword highlight.
- **Sub-headings & UI text (`h2`, nav, buttons, prices):** Gopher Regular is the workhorse secondary face — it appears far more often than the h1 face. An `<em>` inside an `h2` gets a 3px Primary-green underline instead of a color change.
- **`h3` as pull-quote:** Gopher Regular at 1.375em, but rendered **bold and italic** — the only heading level styled as an emphasis/pull-quote rather than a plain heading. (Italic style is a manual override in markup; it is not encoded as a token property.)
- **`h4` and body copy:** Avenir Book. `h4` reuses the body face rather than a heading face, since it's used for small card titles (case-study boxes, course cards) that shouldn't compete with true headings.
- **Code:** JetBrains Mono, used exclusively inside `<pre>`/code blocks — the one place monospace surfaces explicitly.

The type scale is **em-based off an 18.7px (14pt) root size** (`spacing.base`), not a fixed px scale — this lets a component's local font-size context (e.g. a button nested inside a smaller card) rescale its own children proportionally. Body copy uses loose tracking (`0.02rem` letter-spacing) and generous leading (`1.75rem` line-height) for long-form article readability.

**Emphasis is context-dependent by design:** `<em>` means "brand highlight" and renders differently depending on context — bold+italic in body paragraphs, bold-only inside list items, color/underline inside headings. `<strong>` is the plain, context-independent bold tag. Don't treat `<em>` as pure italic; treat it as "apply this context's highlight treatment."

## Layout

Built on a 12-column responsive grid (container / row / col), with `section` as the standard vertical-rhythm unit (`spacing.section`, 3em top/bottom padding). There is no fluid/fixed distinction beyond the grid's own breakpoints — the same container widths are used site-wide.

Spacing follows an em/rem-relative scale rather than a fixed px scale, so spacing compounds correctly with the em-based type scale above:

- `xs` (0.25rem) — micro-adjustments (tag padding, checkbox details).
- `sm` (0.5rem) — tight internal gaps.
- `md` (1em) — default paragraph/element rhythm.
- `lg` (1.25em) — heading margins, standard block spacing.
- `xl` (2em) — article image padding, box padding-top.
- `section` (3em) — vertical padding between page sections; the largest, most consistently applied spacing value in the system.

Full-bleed section backgrounds (fixed-attachment photographic or code-editor imagery) are always paired with the `ink` (dark) section theme, never with light surfaces — they exist to reinforce the "developer culture" mood at key moments (hero, CTA sections), not as general-purpose decoration.

## Elevation & Depth

Depth is conveyed sparingly and only on discrete cards, never on full sections. Course/team cards use a soft, tight drop shadow (`0.5rem 0.5rem 1rem -1rem rgba(0,0,0,.75)`) that reads as a faint lift rather than a heavy shadow. Case-study cards use a slightly more pronounced shadow (`0 4px 8px rgba(0,0,0,.2)`) that intensifies on hover (`0 8px 16px rgba(0,0,0,.2)`) as the sole hover feedback. Form inputs use a colored glow rather than a neutral shadow: focusing a text input produces a soft Primary-green `box-shadow` bloom instead of a browser-default blue ring.

Outside of cards and focus states, hierarchy is conveyed through the section background-theme system (Ink / Surface / Surface Subtle / Surface Tint / Contrast) rather than through shadows — a new dark or tinted section reads as a new "layer" of the page.

## Shapes

Two shape languages coexist deliberately: **soft/pill** for anything actionable, and **flat/square** for content containers and inputs.

- **Pills (`rounded.lg`, 2rem):** Every button variant, the nav CTA, and form submit buttons are fully pill-shaped with a thick (3px) border. This is the strongest "this is clickable" signal in the system.
- **Flat/square (`rounded.none`):** Cards, boxes, and form text inputs are square-cornered — deliberately *not* pill-shaped, so that "container" and "action" never look alike.
- **The signature flap box:** The system's one distinctive custom shape is a thick Primary-green-bordered card whose corners can independently receive an oversized quarter-circle "flap" radius (`rounded.full`) — one, some, or all four corners. This asymmetric rounded-corner treatment is reserved for value-proposition and case-study-info cards and should not be replicated as a generic border-radius; it's a named, intentional brand shape, not a scale value.
- **Small radius (`rounded.sm`, 5px):** Reserved for tag/pill-adjacent chips that are informational rather than actionable (topic tags).
- **Circular (`rounded.full`):** Avatars, carousel prev/next controls, and the flap-box corner treatment above.

## Components

- **`button-primary`** — solid Amber fill, pill-shaped, 3px border, Gopher Regular label. The default call-to-action. Text color flips to Ink or white depending on the section theme it sits in.
- **`button-accent`** — solid Primary-green fill, no border. A higher-emphasis CTA than `button-primary`; use when a single action must outrank a nearby amber button (e.g. a green "primary" button next to a gold "secondary" one).
- **`button-dark`** — solid Ink fill, Surface-Tint text, no border. For CTAs placed on light/tinted backgrounds where Amber would clash with surrounding warm imagery.
- **`button-hollow`** — transparent fill, Amber border and text. The secondary/low-emphasis action alongside a solid button.
- **`nav-button`** — solid Amber pill, smaller (`label-md`) than body CTAs, no border, no trailing arrow (unlike the button family below).
- **Trailing-arrow convention:** on viewports ≥992px, every `button-*` variant (except `nav-button`) appends a small arrow glyph after its label, color-matched to the button (dark arrow on light/amber buttons, white arrow on dark-context buttons, amber arrow on hollow buttons). Bulleted list items inside long-form articles reuse the same arrow glyph as their bullet mark — treat the arrow as a recurring brand motif, not a button-only detail.
- **`input`** — transparent background, flat corners, thick (2px) Primary-green border, green glow on focus. On dark sections the border switches to white. Checkboxes are fully custom-drawn (no native control styling).
- **`tag`** — small pill/chip with a `currentColor` border; comes in light-on-dark and dark-on-light variants plus a compact size for metadata rows.
- **`card`** — shared recipe behind course, team, and case-study cards: cover-image header block + padded body + soft shadow (see Elevation). Treat these as one card recipe reskinned per context, not three separate components.
- **`box`** — the flap-cornered container described in Shapes; also used in a borderless "info" variant (icon + muted `h4`/`p`) for case-study metadata.
- **`price`** — large (`h2`-scale) numeral rendered as a stroke-outlined glyph (Surface-Tint fill, Primary-green 3px text-stroke) rather than solid text; a struck-through Amber numeral above it represents the "before" price.

## Do's and Don'ts

- Do treat Signal Green as the "proof/affirmative" color (checkmarks, success, submit) and Amber as the "navigate/click" color — don't use them interchangeably as competing CTAs on the same screen.
- Do keep the trailing-arrow glyph on `button-*` variants at desktop widths; don't add it to `nav-button` or to `button-hollow`'s non-desktop rendering.
- Do pair full-bleed photographic/code-editor backgrounds only with the `ink` (dark) section theme; don't place them behind `surface` or `surface-subtle` sections.
- Do reserve the flap-corner `box` shape for value-prop and case-study-info cards; don't apply oversized corner radii to ordinary cards or buttons — it's a named brand shape, not a general scale value.
- Do keep pill shapes (`rounded.lg`/`rounded.full`) exclusive to actionable elements (buttons, tags, avatars); don't round the corners of cards or text inputs to match.
- Do use JetBrains Mono only for literal code/`<pre>` content; don't use it for UI labels or headings even in "developer-flavored" contexts.
- Do let `<em>` take on its context's highlight treatment (heading color/underline, body bold-italic, list bold); don't restyle it as plain italic everywhere.
- Don't mix `button-primary` (Amber) and `button-accent` (Green) as two competing primary actions in the same section — pick one per section based on which action matters more.
