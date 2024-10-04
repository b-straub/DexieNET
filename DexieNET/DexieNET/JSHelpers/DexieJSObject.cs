/*
DexieJSObject.cs

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

using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DexieNET
{
    public class DexieJSObject(IJSInProcessObjectReference module, IJSInProcessObjectReference? reference)
    {
        public IJSInProcessObjectReference Module { get; } = module;
        public IJSInProcessObjectReference? Reference { get; private set; } = reference;

        ~DexieJSObject()
        {   
            Reference?.Dispose();
        }
        
        public void SetReference(IJSInProcessObjectReference? reference)
        {
            if (Reference is not null && Reference.Equals(reference))
            {
                return;
            }
            
            Reference?.Dispose();
            Reference = reference;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, params object?[]? args)
        {
            if (Reference is null)
            {
                throw new InvalidDataException("Set reference first.");
            }

            return Reference.InvokeAsync<TValue>(identifier, args);
        }

        public ValueTask InvokeVoidAsync(string identifier, params object?[]? args)
        {
            if (Reference is null)
            {
                throw new InvalidDataException("Set reference first.");
            }

            return Reference.InvokeVoidAsync(identifier, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Mimic base API")]
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, params object?[]? args)
        {
            if (Reference is null)
            {
                throw new InvalidDataException("Set reference first.");
            }

            return Reference.InvokeAsync<TValue>(identifier, cancellationToken, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Mimic base API")]
        public ValueTask InvokeVoidAsync(string identifier, CancellationToken cancellationToken, params object?[]? args)
        {
            if (Reference is null)
            {
                throw new InvalidDataException("Set reference first.");
            }

            return Reference.InvokeVoidAsync(identifier, cancellationToken, args);
        }

        public TValue Invoke<TValue>(string identifier, params object?[]? args)
        {
            if (Reference is null)
            {
                throw new InvalidDataException("Set reference first.");
            }

            return Reference.Invoke<TValue>(identifier, args);
        }
        
        public void InvokeVoid(string identifier, params object?[]? args)
        {
            if (Reference is null)
            {
                throw new InvalidDataException("Set reference first.");
            }
            
            Reference.InvokeVoid(identifier, args);
        }
    }

    internal static class JsonExtensions
    {
         static readonly JsonSerializerOptions Options = new()
         {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static JsonElement? FromObject(this object? element)
        {
            if (element is null)
            {
                return null;
            }

            var json = JsonSerializer.SerializeToElement(element, Options);
            return json;
        }
    }
}
