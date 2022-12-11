import { liveQuery } from 'dexie';
const liveQueryObservables = {};
const liveQuerySubscription = {};
// LiveQuery
export function LiveQuery(dotnetRef, id) {
    const query = liveQuery(() => { dotnetRef.invokeMethod('LiveQueryCallback'); });
    liveQueryObservables[id] = query;
}
export function LiveQuerySubscribe(id) {
    if (id in liveQueryObservables) {
        let query = liveQueryObservables[id];
        let subscription = query.subscribe();
        liveQuerySubscription[id] = subscription;
    }
}
export function LiveQueryUnsubscribe(id) {
    if (id in liveQuerySubscription) {
        let subscription = liveQuerySubscription[id];
        subscription.unsubscribe();
        delete liveQuerySubscription[id];
    }
}
//# sourceMappingURL=dexieNETLiveQuery.js.map