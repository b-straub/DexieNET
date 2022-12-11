using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace DexieNET.Component
{
    public class DexieNET<T> : ComponentBase where T : DBBase, IDBBase
    {
        [Inject]
        protected IDexieNETService<T>? DexieNETService { get; set; }

        [NotNull] // OnInitializedAsync will throw
        public T? Dexie { get; private set; }

        protected async override Task OnInitializedAsync()
        {
            if (DexieNETService is not null)
            {
                Dexie = await DexieNETService.DexieNETFactory.Create();

                if (Dexie is null)
                {
                    throw new InvalidOperationException("Can not create database.");
                }
            }

            await base.OnInitializedAsync();
        }
    }
}
