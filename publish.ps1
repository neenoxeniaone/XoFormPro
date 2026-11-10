# Builds the React app, embeds it into the API's wwwroot, and publishes the API.
# Result: a single app in .\publish that serves BOTH the SPA and the API.
# Deploy .\publish to the IIS site for https://xoformpro.xeniaone.com (see DEPLOYMENT.md).

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "1/4  Building frontend (.env.production, served at site root)..." -ForegroundColor Cyan
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

Write-Host "4/4  Done. Copy '$out' to the IIS site (keep the server's wwwroot\uploads)." -ForegroundColor Green
if (Test-Path "$out\appsettings.Production.json") {
    Write-Host "     Secrets: included from appsettings.Production.json (git-ignored)." -ForegroundColor Yellow
} else {
    Write-Host "     WARNING: no appsettings.Production.json. The host must set ConnectionStrings__Postgres" -ForegroundColor Red
    Write-Host "     and Jwt__SigningKey as environment variables, or the app will not start." -ForegroundColor Red
}
