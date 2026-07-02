# Copilot Instructions

## Project Guidelines
- In the dnGrep codebase, prefer using Guid instead of string for identifier fields (e.g., GrepMatch.FileMatchId, GrepSearchResult.Id) to avoid unnecessary string allocations from Guid.NewGuid().ToString().