$jsonConfigFile = "..\Properties\launchSettings.json"
$JSON = Get-Content $jsonConfigFile | Out-String | ConvertFrom-Json
$urls = $JSON.profiles.https.applicationUrl
$urlArray = $urls.Split(";")

$dexieCloudFile = "dexie-cloud.json"
$importFile = "importfile.json"

if (-not(Test-Path -Path $dexieCloudFile -PathType Leaf)) {
    Write-Host "
    Please run:
    npx dexie-cloud create
    or:
    npx dexie-cloud connect <DB-URL>
    ...to create a database in the cloud
    Then retry this script!
    "
    exit
}

Write-Host "Adding demo users to your application..."
npx dexie-cloud import $importFile

Write-Host "Whitelisting origin: "$url

foreach ($url in $urlArray)
{
    npx dexie-cloud whitelist $url
}

