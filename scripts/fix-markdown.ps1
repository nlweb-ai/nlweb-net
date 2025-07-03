#!/usr/bin/env pwsh

# Markdown formatting fix script for NLWebNet repository
# Fixes common markdown formatting issues automatically

param(
    [switch]$DryRun = $false
)

Write-Host "Scanning for markdown files..." -ForegroundColor Cyan

# Get all markdown files
$markdownFiles = Get-ChildItem -Path . -Recurse -Include "*.md" | Where-Object { 
    $_.FullName -notlike "*node_modules*" -and 
    $_.FullName -notlike "*bin*" -and 
    $_.FullName -notlike "*obj*"
}

Write-Host "Found $($markdownFiles.Count) markdown files" -ForegroundColor Green

$totalIssues = 0

foreach ($file in $markdownFiles) {
    Write-Host "`nProcessing: $($file.Name)" -ForegroundColor Yellow
    
    $content = Get-Content -Path $file.FullName -Raw
    $originalContent = $content
    $fileIssues = 0
    
    # Fix 1: Remove trailing spaces (MD009)
    $beforeTrailing = $content
    $content = $content -split "`n" | ForEach-Object { $_.TrimEnd() } | Join-String -Separator "`n"
    if ($content -ne $beforeTrailing) {
        $fileIssues++
        Write-Host "  Fixed trailing spaces" -ForegroundColor Green
    }
    
    # Fix 2: Ensure consistent list formatting (use dashes for unordered lists)
    $beforeLists = $content
    $content = $content -replace '^(\s*)\*(\s+)', '$1-$2' -replace '^(\s*)\+(\s+)', '$1-$2'
    if ($content -ne $beforeLists) {
        $fileIssues++
        Write-Host "  Fixed list formatting" -ForegroundColor Green
    }
    
    # Fix 3: Remove multiple consecutive blank lines (MD012)
    $beforeBlankLines = $content
    $content = $content -replace '(\r?\n){3,}', "`n`n"
    if ($content -ne $beforeBlankLines) {
        $fileIssues++
        Write-Host "  Fixed multiple blank lines" -ForegroundColor Green
    }
    
    # Fix 4: Ensure file ends with newline (MD047)
    if (-not $content.EndsWith("`n")) {
        $content += "`n"
        $fileIssues++
        Write-Host "  Added final newline" -ForegroundColor Green
    }
    
    # Write the fixed content
    if ($content -ne $originalContent) {
        if (-not $DryRun) {
            Set-Content -Path $file.FullName -Value $content -NoNewline
            Write-Host "  Saved $fileIssues fixes" -ForegroundColor Cyan
        } else {
            Write-Host "  Would save $fileIssues fixes (dry run)" -ForegroundColor Yellow
        }
        $totalIssues += $fileIssues
    } else {
        Write-Host "  No issues found" -ForegroundColor Gray
    }
}

Write-Host "`nMarkdown formatting complete!" -ForegroundColor Green
Write-Host "Total issues fixed: $totalIssues" -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "This was a dry run. Use 'scripts/fix-markdown.ps1' to apply fixes." -ForegroundColor Yellow
} else {
    Write-Host "Running markdownlint to verify fixes..." -ForegroundColor Cyan
    $result = markdownlint . --ignore node_modules --ignore bin --ignore obj 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "All markdown files now pass linting!" -ForegroundColor Green
    } else {
        Write-Host "Some issues remain:" -ForegroundColor Yellow
        Write-Host $result
    }
}
