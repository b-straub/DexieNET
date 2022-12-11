/*
dexieNET.js

Copyright(c) 2022 Bernhard Straub

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


import Dexie, {
    DBCore, DBCoreMutateRequest, DBCoreMutateResponse, DBCoreTable, liveQuery, Middleware, Observable, Subscription, TransactionMode
} from 'dexie';

export class DB extends Dexie {
    constructor(name: string) {
        super(name);
    }
}

export function Create(name: string): DB {
    let db = new DB(name);
    db.use(NETMiddleware);
    return db;
}

export function Delete(name: string): Promise<void> {
    return Dexie.delete(name);
}

export function Name(): string {
    return Dexie.name;
}

export function Version(db: DB): number {
    return db.verno;
}

const NETMiddleware: Middleware<DBCore> = {
    stack: 'dbcore',
    name: 'NETMiddleware',
    create: createNETMiddleware,
}

// middleware to remove c# null values for autoincrement primary keys
// this will allow inbound autoincrement keys for c# objects
function createNETMiddleware(core: DBCore): DBCore {
    return {
        ...core,
        table(tableName: string): DBCoreTable {
            const table = core.table(tableName)
            return {
                ...table,
                async mutate(req: DBCoreMutateRequest): Promise<DBCoreMutateResponse> {
                    if (req.type === 'add') {
                        const addRequest = { ...req };
                        const { primaryKey } = table.schema;

                        if (primaryKey.autoIncrement && primaryKey.keyPath != null) {
                            const keys = new Array<string>().concat(primaryKey.keyPath);
                            keys.map(k => {
                                req.values = req.values.map(v => {
                                    if (typeof v === 'object' && v.hasOwnProperty(k)) {
                                        if (v[k] === null) {
                                            delete v[k];
                                        }
                                    }
                                })
                            });
                        }

                        return table.mutate(addRequest);
                    }

                    return table.mutate(req);
                }
            }
        }
    };
}