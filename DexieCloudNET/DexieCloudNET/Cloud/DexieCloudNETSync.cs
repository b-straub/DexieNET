﻿/*
DexieNETCloudSync.cs

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

namespace DexieCloudNET
{
    public record SyncState(SyncState.SyncStatus Status, SyncState.SyncStatePhase Phase, int Progress, string? Error)
    {
        public enum SyncStatus
        {
            NOT_STARTED,
            CONNECTING,
            CONNECTED,
            DISCONNECTED,
            ERROR,
            OFFLINE
        }

        public enum SyncStatePhase
        {
            INITIAL,
            NOT_IN_SYNC,
            PUSHING,
            PULLING,
            IN_SYNC,
            ERROR,
            OFFLINE
        }
    }

    public record PersistedSyncState(
        string? ServerRevision,
        Dictionary<string, long> LatestRevisions,
        string[] Realms,
        string[] InviteRealms,
        string ClientIdentity,
        bool? InitiallySynced,
        string? RemoteDbId,
        string[] SyncedTables,
        DateTime? Timestamp,
        string? Error
    );

    public record SyncOptions(SyncOptions.SyncPurpose Purpose = SyncOptions.SyncPurpose.PUSH, bool Wait = true)
    {
        public enum SyncPurpose
        {
            PUSH,
            PULL
        }
    }
}
