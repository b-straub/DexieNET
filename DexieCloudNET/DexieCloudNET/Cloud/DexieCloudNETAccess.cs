/*
DexieNETCloudAccesss.cs

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

using System.Linq.Expressions;
using DexieNET;

// ReSharper disable once CheckNamespace
namespace DexieCloudNET
{
    // records are not actually compiled to DB stores but are prebuild in SG when any table with '[Schema(CloudSync = true)]' exist in DB
    public record Permission
    (
        [property: PermissionArrayConverter] string[]? Add,
        [property: PermissionDictionaryConverter] Dictionary<string, string[]>? Update,
        [property: PermissionArrayConverter] string[]? Manage
    )
    {
        public Permission() : this(null, null, null)
        {
        }
    }

    [Schema(CloudSync = true)]
    public record Realm
    (
        string Name,
        string? Represents = null,
        string? Owner = null,
        [property: Index(IsPrimary = true, IsAuto = true)] string? RealmId = null
    ) : IDBStore, IDBCloudEntity;

    [Schema(CloudSync = true)]
    [CompoundIndex("RealmId", "Email")]
    [CompoundIndex("UserId", "Email")]
    public record Member : IDBStore, IDBCloudEntity
    {
        [Index]
        public string? UserId { get; init; } = null;
        public string? Email { get; init; }
        public string? Name { get; init; }
        public bool? Invite { get; init; }
        public bool? Invited { get; init; } = null;
        public DateTime? Accepted { get; init; } = null;
        public DateTime? Rejected { get; init; } = null;
        public DateTime? InvitedDate { get; init; } = null; // Set by system in processInvites
        public InvitedBy? InvitedBy { get; init; } = null; // Set by system in processInvites
        public string[]? Roles { get; init; }
        public Permission? Permissions { get; init; } = null;

        [Index(IsPrimary = true, IsAuto = true)]
        public string? Id { get; init; } = null;
        public string? RealmId { get; init; }
        public string? Owner { get; init; } = null;
        
        public Member()
        {
        }

        public Member(string realmId)
        {
            RealmId = realmId;
        }

        public Member(string realmId, string name, string eMail, bool invite, string[]? roles)
        {
            RealmId = realmId;
            Name = name;
            Email = eMail;
            Invite = invite;
            Roles = roles;
        }
    };

    public record InvitedBy
    (
       string Name,
       string Email,
       string UserId
    );

    public record InviteRealm : Realm
    {
        public Permission Permission { get; init; }

        public InviteRealm(string Name, string? Represents = null, string? Owner = null, string? RealmId = null, Permission? permission = null) : base(Name, Represents, Owner, RealmId)
        {
            Permission = permission ?? new Permission();
        }
    }
    public record Invite : Member
    {
        public InviteRealm? Realm { get; init; }
    }

    [Schema(CloudSync = true)]
    [CompoundIndex("RealmId", "Name", IsPrimary = true)]
    public record Role
    (
        string Name,
        Permission Permissions,
        string RealmId,
        string? Owner = null,
        string? Description = null,
        string? DisplayName = null,
        long? SortOrder = null
    ) : IDBStore;
    
    public static class PermissionExtensions
    {
        public static Permission WithAdd<T, I>(this Permission permission, Table<T, I> table) where T : IDBStore
        {
            if (permission.Add is null)
            {
                permission = permission with { Add = new[] { table.Name } };
            }
            else
            {
                permission = permission with { Add = [.. permission.Add, table.Name] };
            }
            return permission;
        }

        public static Permission WithAddAll(this Permission permission)
        {
            if (permission.Add is null)
            {
                permission = permission with { Add = new[] { "*" } };
            }
            else if (permission.Add.Length != 1 || permission.Add.FirstOrDefault() != "*")
            {
                throw new InvalidOperationException("Wildcard for Add already exist!");
            }
            return permission;
        }

        public static Permission WithUpdate<T, I, Q>(this Permission permission, Table<T, I> table, Expression<Func<T, Q>> query) where T : IDBStore
        {
            if (permission.Update is null)
            {
                permission = permission with { Update = [] };
            }

            permission.Update.Add(table.Name, new[] { query.GetKey() });
            return permission;
        }

        public static Permission WithUpdateAllTables(this Permission permission)
        {
            if (permission.Update is null)
            {
                permission = permission with { Update = [] };
            }
            else if (permission.Update.TryGetValue("*", out string[]? value))
            {
                if (value.Length != 1 || value.FirstOrDefault() != "*")
                {
                    throw new InvalidOperationException("Wildcard for UpdateAllTables already exist!");
                }
            }

            permission.Update.Add("*", new[] { "*" });
            return permission;
        }

        public static Permission WithUpdateAllProperties<T, I>(this Permission permission, Table<T, I> table) where T : IDBStore
        {
            if (permission.Update is null)
            {
                permission = permission with { Update = [] };
            }
            else if (permission.Update.TryGetValue(table.Name, out string[]? value))
            {
                if (value.Length != 1 || value.FirstOrDefault() != "*")
                {
                    throw new InvalidOperationException("Wildcard for UpdateAllProperties already exist!");
                }
            }

            permission.Update.Add(table.Name, new[] { "*" });
            return permission;
        }

        public static Permission WithManage<T, I>(this Permission permission, Table<T, I> table) where T : IDBStore
        {
            if (permission.Manage is null)
            {
                permission = permission with { Manage = new[] { table.Name } };
            }
            else
            {
                permission = permission with { Manage = [.. permission.Manage, table.Name] };
            }
            return permission;
        }

        public static Permission WithManageAll(this Permission permission)
        {
            if (permission.Manage is null)
            {
                permission = permission with { Manage = new[] { "*" } };
            }
            else if (permission.Manage.Length != 1 || permission.Manage.FirstOrDefault() != "*")
            {
                throw new InvalidOperationException("Wildcard for Manage already exist!");
            }
            return permission;
        }
    }
}