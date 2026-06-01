# UI Presentation Guide

This guide defines the visual rules for the Survival List Overlay. The overlay is an in-game support tool, so compact clarity wins over decoration.

## Layout Priorities

- Keep the overlay narrow enough to sit beside gameplay without dominating the screen.
- Divide the window into three clear zones: header/status, quick-add/search, and tracked entries.
- Make tracked entries the visual center after items are added.
- Let search results feel secondary unless the user is actively searching.
- Preserve mouse and keyboard parity for every common action.

## Spacing And Alignment

- Use 4px increments for small gaps and 8px increments for section gaps.
- Keep outer panel padding at 10px or 12px.
- Keep repeated row controls aligned by using stable grid columns.
- Avoid text labels that change button width between states.
- Use fixed-size action buttons for repeated row actions.

## Text Hierarchy

- Primary text: item or recipe name, white, bold, compact.
- Secondary text: type labels, remaining counts, ingredient summaries, muted gray.
- Avoid adding instructional text inside the app unless it is an empty state.
- Keep labels short enough to avoid wrapping inside controls.

## Row Rules

- Every tracked entry row should have three visual groups: identity, progress, actions.
- Quantity controls should stay near progress values.
- Priority controls should be visually quieter than quantity controls.
- Favorite, sticky, and remove actions should be compact and consistently aligned.
- Selected rows should remain readable against the transparent overlay background.

## Recipe Rules

- Recipe parents remain top-level entries.
- Recipe ingredient rows must be indented and visually quieter than parent rows.
- Expanded recipe content may increase row height; normal item rows should remain consistent.
- Ingredient rows do not get the same action weight as parent rows.

## Screenshot Audit Expectations

Screenshot analysis is a diagnostic aid, not the design authority. Use it to identify:

- content too close to edges,
- uneven horizontal or vertical spacing,
- inconsistent row heights,
- dense regions with too many controls,
- obvious low-contrast areas,
- clipping risk.

Generated audit reports belong under `artifacts/ui_audit/` and should not be committed.
