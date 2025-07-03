#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fix numbered lists to use auto-numbering (1. 1. 1.) format
.DESCRIPTION
    Converts all numbered lists from sequential numbering (1. 2. 3.) to 
    auto-numbering (1. 1. 1.) format to prevent numbering errors and
    improve maintainability.
#>

param(
    [string]$Path = ".",
    [switch]$WhatIf
)

Write-Host "🔧 Fixing numbered lists in markdown files..." -ForegroundColor Blue

# Find all markdown files (including hidden folders)
$markdownFiles = Get-ChildItem -Path $Path -Recurse -Filter "*.md" -Force | Where-Object { 
    -not $_.FullName.Contains("node_modules") -and 
    -not $_.FullName.Contains(".git\") 
}

$totalFixed = 0
$filesModified = 0

foreach ($file in $markdownFiles) {
    $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    $fileFixed = 0
    
    # Convert numbered lists: 2. -> 1., 3. -> 1., etc.
    # Match lines that start with a number (2-9) followed by a dot and space
    $pattern = '(?m)^(\s*)([2-9])\.\s'
    $replacement = '$1' + '1. '
    
    $content = $content -replace $pattern, $replacement
    
    if ($content -ne $originalContent) {
        $matches = [regex]::Matches($originalContent, $pattern)
        $fileFixed = $matches.Count
        $totalFixed += $fileFixed
        $filesModified++
        
        $relativePath = $file.FullName.Replace((Get-Location).Path, "").TrimStart('\')
        Write-Host "  ✅ $relativePath - Fixed $fileFixed numbered list items" -ForegroundColor Green
        
        if (-not $WhatIf) {
            Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
        }
    }
}

Write-Host ""
if ($WhatIf) {
    Write-Host "🔍 Would fix $totalFixed numbered list items in $filesModified files" -ForegroundColor Yellow
    Write-Host "   Run without -WhatIf to apply changes" -ForegroundColor Yellow
} else {
    Write-Host "✅ Fixed $totalFixed numbered list items in $filesModified files" -ForegroundColor Green
    Write-Host "🔍 Running markdownlint to verify..." -ForegroundColor Blue
    
    # Run markdownlint to check if we fixed everything
    $lintResult = & markdownlint --config .markdownlint.json **/*.md 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ All markdown files now pass linting!" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Some markdown issues remain:" -ForegroundColor Yellow
        Write-Host $lintResult
    }
}
