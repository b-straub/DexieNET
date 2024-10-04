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
using DexieNET;

namespace DexieCloudNET
{
    public sealed class DexieCloudNETFactory<T> : IDexieNETFactory<T> where T : IDBBase
    {
        private IJSInProcessObjectReference? _module;
        private IJSInProcessObjectReference? _cloud;
        
        private readonly IJSRuntime _jsRuntime;
        public DexieCloudNETFactory(IJSRuntime jsRuntime)
        {
            if (!OperatingSystem.IsBrowser())
            {
                throw new InvalidOperationException("This IndexedDB wrapper is only designed for Webassembly usage!");
            }

            _jsRuntime = jsRuntime;
        }

        public async ValueTask<T> Create()
        {
            _module ??= await _jsRuntime.InvokeAsync<IJSInProcessObjectReference>("import", @"./_content/DexieNET/js/dexieNET.js");
            _cloud ??= await _jsRuntime.InvokeAsync<IJSInProcessObjectReference>("import", @"./_content/DexieCloudNET/js/dexieCloudNET.js");
            
            var reference = await _cloud.InvokeAsync<IJSInProcessObjectReference>("CreateCloud", T.Name);

            return (T)T.Create(_module, reference, _cloud);
        }

        public async ValueTask Delete()
        {
            _module ??= await _jsRuntime.InvokeAsync<IJSInProcessObjectReference>("import", @"./_content/DexieNET/js/dexieNET.js");
            await _module.InvokeVoidAsync("Delete", T.Name);
        }
        
        public void Dispose()
        {
            _cloud?.Dispose();
            _module?.Dispose();
        }
    }

    public sealed class DexieCloudNETService<T>(IJSRuntime jsRuntime) : IDexieNETService<T> where T : DBBase, IDBBase
    {
        public IDexieNETFactory<T> DexieNETFactory { get; } = new DexieCloudNETFactory<T>(jsRuntime);
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDexieCloudNET<T>(this IServiceCollection services) where T : DBBase, IDBBase
        {
            return services.AddSingleton<IDexieNETService<T>, DexieCloudNETService<T>>();
        }
    }
}