using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using DexieNET;

namespace DexieCloudNET.Component
{
    public class DexieCloudNET<T>(string cloudURL) : OwningComponentBase where T : DBBase, IDBBase
    {
        [Inject]
        protected IDexieNETService<T>? DexieNETService { get; set; }

        [NotNull] // OnInitializedAsync will throw
        protected T? Dexie { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if (DexieNETService is not null)
            {
                DexieCloudOptions cloudOptions = new(cloudURL);

                Dexie = await DexieNETService.DexieNETFactory.Create();

                if (Dexie is null)
                {
                    throw new InvalidOperationException("Can not create database.");
                }

                await Dexie.ConfigureCloud(cloudOptions);
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
