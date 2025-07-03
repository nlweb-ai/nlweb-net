#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fix MD012 multiple consecutive blank lines across all markdown files
.DESCRIPTION
    Reduces sequences of more than 2 consecutive blank lines to exactly 2 blank lines
#>

Write-Host "🔧 Fixing MD012 multiple blank lines across all markdown files..." -ForegroundColor Blue

$fixed = 0
$filesFixed = 0

Get-ChildItem -Path . -Recurse -Filter "*.md" | Where-Object { 
    -not $_.FullName.Contains("node_modules") -and 
    -not $_.FullName.Contains(".git") 
} | ForEach-Object {
    $file = $_.FullName
    $relativePath = $file.Replace((Get-Location).Path, "").TrimStart('\')
    
    $content = Get-Content -Path $file -Raw -Encoding UTF8
    $originalContent = $content
    
    # Count existing issues
    $lines = $content -split "`r?`n"
    $blanks = 0
    $issues = 0
    for($i = 0; $i -lt $lines.Length; $i++) {
        if($lines[$i] -match '^\s*$') {
            $blanks++
        } else {
            if($blanks -gt 2) {
                $issues++
            }
            $blanks = 0
        }
    }
    
    if($issues -gt 0) {
        # Fix by replacing 3+ consecutive blank lines with exactly 2
        $content = $content -replace '(\r?\n\s*\r?\n\s*\r?\n)(\s*\r?\n)*', "`r`n`r`n"
        
        Set-Content -Path $file -Value $content -Encoding UTF8 -NoNewline
        Write-Host "  ✅ $relativePath - Fixed $issues MD012 issues" -ForegroundColor Green
        $fixed += $issues
        $filesFixed++
    }
}

Write-Host ""
Write-Host "✅ Fixed $fixed MD012 issues in $filesFixed files" -ForegroundColor Green
