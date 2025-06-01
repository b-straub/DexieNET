/*
dexieNETLiveQuery.ts

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

import {liveQuery} from 'dexie';
import {from, Observable, Subscription} from 'rxjs';

const liveQueryObservables: { [id: number]: Observable<any>; } = {};
const liveQuerySubscription: { [id: number]: Subscription; } = {};

// LiveQuery
export function LiveQuerySubscribe(dotnetRef: any): number {

    const id = Date.now();
    liveQueryObservables[id] = from(liveQuery(async () => {
        return await dotnetRef.invokeMethodAsync('LiveQueryCallback');
    }));

    liveQuerySubscription[id] = liveQueryObservables[id].subscribe({
        next: (v) => dotnetRef.invokeMethod('OnNextJS', v),
        error: (e) => dotnetRef.invokeMethod('OnErrorJS', e.message),
        complete: () => dotnetRef.invokeMethod('OnCompletedJS')
    });

    return id;
}

export function LiveQueryUnsubscribe(id: number): void {

    if (id in liveQuerySubscription) {
        liveQuerySubscription[id].unsubscribe();
        delete liveQuerySubscription[id];
    }
}
