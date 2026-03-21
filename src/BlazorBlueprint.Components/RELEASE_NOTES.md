## What's New in v3.7.0

### New Features

- **Infinite scroll** support for **Combobox**, **Select**, **MultiSelect**, and their FormField variants via `OnLoadMore`, `IsLoading`, and `EndOfListMessage` parameters
- **BbCommandList** now supports infinite scroll with `OnLoadMore`, `IsLoading`, and `EndOfListMessage` parameters
- **BbSelectContent** supports infinite scroll directly for compositional usage
- **MultiSelect** `SearchQueryChanged` parameter for server-side / external filtering — when set, the component bypasses its internal text filter and trusts the consumer to control displayed items
- **FormFieldCombobox** now passes through `SearchQuery`, `SearchQueryChanged`, `OnLoadMore`, `IsLoading`, `EndOfListMessage`, and `ActiveClass` parameters

### Bug Fixes

- **FormFieldCombobox** search clearing bug fixed — local `SearchQuery` now stays in sync when `SearchQueryChanged` is intercepted
- **BbCommandEmpty** now correctly shows "no results" when external filtering (`FilterFunction`) is active and the item list is empty
- **MultiSelect** space key no longer steals input when no item is focused, allowing spaces to be typed in the search field
- **MultiSelect** badge display text now survives `Options` array changes during async filtering via an internal display text cache

### Improvements

- **BbSelectContent** refactored to separate outer (border/shadow/animation) and inner (scrollable) containers for correct scroll detection
- **`isNearBottom` scroll utility** moved from Components `data-view.js` to Primitives `element-utils.js` for reuse across all scrollable dropdowns
- Bumped **BlazorBlueprint.Primitives** dependency to 3.7.0
