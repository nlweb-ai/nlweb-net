# Markdown Tools

This folder contains PowerShell scripts for maintaining markdown formatting consistency across the repository.

## Scripts

- **`lint-markdown.ps1`** - Run markdownlint validation on all markdown files
- **`fix-markdown.ps1`** - Automatically fix common markdown formatting issues
- **`fix-numbered-lists.ps1`** - Standardize numbered lists to use "1." format
- **`fix-blank-lines.ps1`** - Fix excessive blank lines in markdown files
- **`fix-md031.ps1`** - Fix fenced code block spacing issues
- **`fix-md012.ps1`** - Fix multiple consecutive blank lines
- **`test-markdownlint.ps1`** - Test markdownlint functionality

## Usage

Run any script from the repository root:

```powershell
# Check markdown formatting
.\scripts\markdown-tools\lint-markdown.ps1

# Fix common formatting issues
.\scripts\markdown-tools\fix-markdown.ps1
```

## Configuration

Markdown linting rules are configured in `.markdownlint.json` at the repository root.
