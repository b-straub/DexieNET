#!/bin/bash -e

if [ ! -f "./dexie-cloud.json" ]; then
    echo "Please run:"
    echo "  npx dexie-cloud create"
    echo "or: "
    echo "  npx dexie-cloud connect <DB-URL>"
    echo "...to create a database in the cloud"
    echo "Then retry this script!"
    exit 1;
fi

echo "Adding demo users to your application..."
npx dexie-cloud import dexie-cloud.json

URLS=$(jq -r '.profiles.https.applicationUrl' ../Properties/launchSettings.json)

IFS=';' read -ra ADDR <<< $URLS 
for i in "${ADDR[@]}"; do
  echo "Whitelisting origin: $i"
  npx dexie-cloud whitelist $i
done