# Builds the React app, embeds it into the API's wwwroot, and publishes the API.
# Result: a single self-contained app in .\publish that serves BOTH the SPA and the API.
# Deploy .\publish to IIS as the "FormSubmissionPro" application.

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "1/4  Building frontend (.env.production -> base /XoFormPro)..." -ForegroundColor Cyan
Push-Location "$root\web"
npm install
npm run build
Pop-Location

Write-Host "2/4  Copying React build into API wwwroot..." -ForegroundColor Cyan
$wwwroot = "$root\api\FormSubmissionPro.Api\wwwroot"
# Clear old SPA assets but keep the uploads folder.
if (Test-Path "$wwwroot\assets") { Remove-Item "$wwwroot\assets" -Recurse -Force }
Get-ChildItem "$wwwroot" -File | Where-Object { $_.Name -ne ".gitkeep" } | Remove-Item -Force
Copy-Item "$root\web\dist\*" "$wwwroot" -Recurse -Force

Write-Host "3/4  Publishing API (Release)..." -ForegroundColor Cyan
$out = "$root\publish"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
dotnet publish "$root\api\FormSubmissionPro.Api\FormSubmissionPro.Api.csproj" -c Release -o $out

Write-Host "4/4  Done. Deploy '$out' to IIS as the XoFormPro application." -ForegroundColor Green
Write-Host "     Set env vars on the host: ConnectionStrings__Postgres, Jwt__SigningKey, App__BaseUrl." -ForegroundColor Yellow
