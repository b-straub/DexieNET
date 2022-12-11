import { liveQuery, Observable, Subscription } from 'dexie';

const liveQueryObservables: { [id: number]: Observable<any>; } = {};
const liveQuerySubscription: { [id: number]: Subscription; } = {};

// LiveQuery
export function LiveQuery(dotnetRef: any, id: number): void {
    const query = liveQuery(() => { dotnetRef.invokeMethod('LiveQueryCallback'); });
    liveQueryObservables[id] = query;
}

export function LiveQuerySubscribe(id: number): void {

    if (id in liveQueryObservables) {
        let query = liveQueryObservables[id];
        let subscription = query.subscribe();
        liveQuerySubscription[id] = subscription;
    }
}

export function LiveQueryUnsubscribe(id: number): void {

    if (id in liveQuerySubscription) {
        let subscription = liveQuerySubscription[id];
        subscription.unsubscribe();
        delete liveQuerySubscription[id];
    }
}
