DexieCloudNETPushServer
========

DexieCloudNETPushServer is a helper application that supports
WebPush messages in a DexieCloudNET based solution.

The PushServer will use the [Dexie Cloud REST API](https://dexie.org/cloud/docs/rest-api) to communicate with any database in a solution agnostic way.
The best way to set up your own PushServer is with Docker.
Create a Docker image using the [buildPushServer.sh](../buildPushServer.sh) script.
Make sure *--platform* matches your desired architecture.

**For new releases please expire all subscriptions and if push still doesn't work delete and reinstall your PWA!**

Setting up the app's secrets is a two-step process:
* map any local directory to */pushserver/database/*
* Place a *secrets.json* file inside the mounted directory with the following content
 ```json
{
  "Databases": [
    {
       "url": "your dexie cloud database url",
       "clientId": "your dexie cloud client id",
       "clientSecret": "your dexie cloud client secret"
    }
],
  "VapidKeys": {
    "publicKey": "Your Vapid public key",
    "privateKey": "your Vapid private key"
  } 
}
```
* create a settings file (rootFolder may be different for *development* and *production*) with the vapid public key and the root folder of the pwa, e.g.
```json
{
   "applicationServerKey": "your Vapid public key",
   "rootFolder": "your folder for the pwa"
}
```
In any case, the pushURL and vapid public key must be added when configuring the cloud database.
Make sure that the resulting push URL will be correctly decoded on one of your pages.

```csharp
public static async Task ConfigureCloud(this DBBase dexie, DexieCloudOptions cloudOptions, 
    string pushURL, string? applicationServerKey = null)
```

See the [DexieNETCloudSample](../DexieNETCloudSample) for more information.

To enable push support, do the following:
* add the [DBAddPushSupport](https://github.com/b-straub/DexieNET/blob/9e0915b38995bce0660229c2b77cc86bc7b6a058/DexieNETCloudSample/Dexie/Services/DexieCloudService.cs#L15) attribute to your *IDBStore
* Store push information in your database like this [AddPushNotification](https://github.com/b-straub/DexieNET/blob/9e0915b38995bce0660229c2b77cc86bc7b6a058/DexieNETCloudSample/Dexie/Services/ToDoItemService.cs#L187)