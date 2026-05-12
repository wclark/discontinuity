param(
  [string]$Profile = "georgist-login",
  [string]$Bucket = "discontinuity.org"
)

$ErrorActionPreference = "Stop"
$Aws = "C:\Program Files\Amazon\AWSCLIV2\aws.exe"
$SitePath = Join-Path $PSScriptRoot "site"

if (-not (Test-Path $Aws)) {
  throw "AWS CLI was not found at $Aws"
}

if (-not (Test-Path $SitePath)) {
  throw "Site path was not found at $SitePath"
}

& $Aws s3 sync $SitePath "s3://$Bucket" `
  --delete `
  --profile $Profile `
  --cache-control "public, max-age=300"

