/*
dexieNETPersistence.ts

Copyright(c) 2024 Bernhard Straub

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

'DexieNET' used with permission of David Fahlander 
*/

// based on https://dexie.org/docs/StorageManager

export async function InitStoragePersistence(): Promise<number> {
    const persist = await tryPersistWithoutPromtingUser();
    switch (persist) {
        case "never":
            return 0;
        case "persisted":
            return 1;
        case "prompt":
            return 2;
        default:
            return -1;
    }
}

/** Tries to convert to persisted storage.
  @returns {Promise<boolean>} Promise resolved with true if successfully
  persisted the storage, false if not, and undefined if the API is not present.
*/
export async function Persist(): Promise<Boolean> {
    return await navigator.storage && navigator.storage.persist ?
        navigator.storage.persist() :
        false;
}

/** Queries available disk quota.
  @see https://developer.mozilla.org/en-US/docs/Web/API/StorageEstimate
  @returns {Promise<{quota: number, usage: number}>} Promise resolved with
  {quota: number, usage: number} or undefined.
*/

const seUndef: StorageEstimate = {
    quota: -1,
    usage: -1
}

export async function ShowEstimatedQuota() : Promise<StorageEstimate> {
    return await navigator.storage && navigator.storage.estimate ?
        navigator.storage.estimate() :
        seUndef;
}

/** Tries to persist storage without ever prompting user.
  @returns {Promise<string>}
    "never" In case persisting is not ever possible. Caller don't bother
      asking user for permission.
    "prompt" In case persisting would be possible if prompting user first.
    "persisted" In case this call successfully silently persisted the storage,
      or if it was already persisted.
*/
export async function tryPersistWithoutPromtingUser(): Promise<string> {
    if (!navigator.storage || !navigator.storage.persisted) {
        return "never";
    }
    let persisted = await navigator.storage.persisted();

    if (persisted) {
        return "persisted";
    }

    // the experimental permission API on iOS doesn't work with "persistent-storage" yet
    if (iOS() || !navigator.permissions || !navigator.permissions.query) {
        return "prompt"; // It MAY be successful to prompt. Don't know.
    }
    
    const permission = await navigator.permissions.query({
        name: "persistent-storage"
    });

    if (permission.state === "granted") {
        persisted = await navigator.storage.persist();
        if (persisted) {
            return "persisted";
        } else {
            throw new Error("Failed to persist");
        }
    }
    if (permission.state === "prompt") {
        return "prompt";
    }
    return "never";
}

export function iOS() {
    return [
        'iPad Simulator',
        'iPhone Simulator',
        'iPod Simulator',
        'iPad',
        'iPhone',
        'iPod'
    ].includes(navigator.platform)
        // iPad on iOS 13 detection
        || (navigator.userAgent.includes("Mac") && "ontouchend" in document)
}
