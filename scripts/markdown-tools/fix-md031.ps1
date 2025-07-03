#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fix MD031 blanks-around-fences issues in markdown files
.DESCRIPTION
    Ensures all fenced code blocks are surrounded by blank lines
#>

param(
    [string]$FilePath = $null
)

Write-Host "🔧 Fixing MD031 blanks-around-fences issues..." -ForegroundColor Blue

$files = if ($FilePath) { 
    @(Get-Item $FilePath) 
} else { 
    Get-ChildItem -Path . -Recurse -Filter "*.md" | Where-Object { 
        -not $_.FullName.Contains("node_modules") -and 
        -not $_.FullName.Contains(".git") 
    }
}

$totalFixed = 0
$filesModified = 0

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    # Fix fenced code blocks that don't have blank lines before them
    # Look for lines that aren't blank followed immediately by ```
    $content = $content -replace '(?m)(?<!^\s*$\r?\n)^(\s*```)', "`r`n`$1"
    
    # Fix fenced code blocks that don't have blank lines after them
    # Look for ``` followed immediately by non-blank lines
    $content = $content -replace '(?m)^(\s*```)\r?\n(?!\s*$)', "`$1`r`n`r`n"
    
    if ($content -ne $originalContent) {
        $relativePath = $file.FullName.Replace((Get-Location).Path, "").TrimStart('\')
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
        Write-Host "  ✅ $relativePath - Fixed MD031 issues" -ForegroundColor Green
        $filesModified++
    }
}

Write-Host ""
Write-Host "✅ Fixed MD031 issues in $filesModified files" -ForegroundColor Green
