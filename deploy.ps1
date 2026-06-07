param(
  [string]$Profile = "georgist-login",
  [string]$Bucket = "discontinuity-website",
  [string]$Region = "us-west-1",
  [string]$DistributionId = "E3V4IEGTIQS7EP"
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

function Assert-AwsSuccess {
  param([string]$Step)

  if ($LASTEXITCODE -ne 0) {
    throw "$Step failed with exit code $LASTEXITCODE."
  }
}

& $Aws s3 sync $SitePath "s3://$Bucket" `
  --delete `
  --profile $Profile `
  --region $Region `
  --cache-control "public, max-age=300"
Assert-AwsSuccess "Syncing site to s3://$Bucket"

if ($DistributionId) {
  & $Aws cloudfront create-invalidation `
    --distribution-id $DistributionId `
    --paths "/*" `
    --profile $Profile
  Assert-AwsSuccess "Creating CloudFront invalidation"
}
