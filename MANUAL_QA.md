# Manual QA Checklist

Use this after UI or workflow changes:

- Start the overlay and confirm it opens transparent, topmost, and compact.
- Confirm locked mode prevents accidental dragging and resizing.
- Toggle edit mode and confirm the title bar appears.
- In edit mode, drag and resize the overlay, restart, and confirm position and size persist.
- Search for an item, add it with a quantity, increment it, decrement it, and remove it.
- Add the same item again and confirm the existing entry updates instead of creating a duplicate.
- Use the explicit duplicate action and confirm it creates a separate top-level entry.
- Search for a recipe, add it, expand it, and confirm ingredient requirements appear.
- Track a recipe and a standalone item that uses the same material, then confirm aggregate material demand is visible.
- Switch to counting mode, add the same item several times, and confirm the collected count accumulates.
- Mark an entry as favorite and verify the favorites filter shows only favorite entries.
- Mark an entry as sticky and verify removal asks for confirmation.
- Change sort mode between priority and alphabetical and verify the visible order changes.
- Use keyboard flow: Ctrl+F focuses search, Up/Down selects search results, Enter adds, +/- adjusts selected quantity, Delete removes, F toggles favorite, S toggles sticky, E expands a recipe, Ctrl+L toggles lock/edit, Ctrl+G toggles favorites, and Ctrl+Tab switches list type.
- Close and reopen the app, then confirm entries, quantities, favorite/sticky flags, priorities, sort mode, and recipe expansion state were persisted.
