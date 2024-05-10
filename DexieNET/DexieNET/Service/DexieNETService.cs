/*
DexieNETService.cs

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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace DexieNET
{
    public interface IDexieNETFactory<T>
    {
        public ValueTask<T> Create();
        public ValueTask Delete();
    }

    public interface IDexieNETService<T> where T : DBBase, IDBBase
    {
        public IDexieNETFactory<T> DexieNETFactory { get; }
    }

    public sealed class DexieNETService<T>(IJSRuntime jsRuntime) : IDexieNETService<T> where T : DBBase, IDBBase
    {
        public IDexieNETFactory<T> DexieNETFactory { get; } = new DexieNETFactory<T>(jsRuntime);
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDexieNET<T>(this IServiceCollection services) where T : DBBase, IDBBase
        {
            services.AddSingleton<IDexieNETService<T>, DexieNETService<T>>();
            return services;
        }
    }
}