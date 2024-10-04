using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace DexieNET.Component
{
    public class DexieNET<T>() : OwningComponentBase where T : DBBase, IDBBase
    {
        [Inject]
        protected IDexieNETService<T>? DexieNETService { get; set; }

        [NotNull] // OnInitializedAsync will throw
        protected T? Dexie { get; private set; }
        
        protected override async Task OnInitializedAsync()
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
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DexieNETService?.DexieNETFactory.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
