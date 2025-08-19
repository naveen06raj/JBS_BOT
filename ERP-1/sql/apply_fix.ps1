$env:PGPASSWORD = 'Magnus123$'

# Try to find psql in common installation paths
$psqlPaths = @(
    "C:\Program Files\PostgreSQL\16\bin\psql.exe",
    "C:\Program Files\PostgreSQL\15\bin\psql.exe",
    "C:\Program Files\PostgreSQL\14\bin\psql.exe",
    "C:\Program Files\PostgreSQL\13\bin\psql.exe",
    "C:\Program Files\PostgreSQL\12\bin\psql.exe"
)

$psqlPath = $null
foreach ($path in $psqlPaths) {
    if (Test-Path $path) {
        $psqlPath = $path
        break
    }
}

if ($null -eq $psqlPath) {
    Write-Host "Please enter the full path to your psql.exe file:"
    $psqlPath = Read-Host
    if (-not (Test-Path $psqlPath)) {
        Write-Host "Invalid path. Please make sure PostgreSQL is installed."
        exit 1
    }
}

Write-Host "Using PostgreSQL at: $psqlPath"
Write-Host "Applying SQL fixes..."

Get-Content "fix_demo_grid_function.sql" | & $psqlPath -h localhost -p 5433 -U postgres -d postgres

if ($LASTEXITCODE -eq 0) {
    Write-Host "SQL fixes applied successfully!"
} else {
    Write-Host "Error applying SQL fixes. Please check the output above for details."
}
