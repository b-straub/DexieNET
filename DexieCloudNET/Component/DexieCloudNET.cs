using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using DexieNET;

namespace DexieCloudNET.Component
{
    public class DexieCloudNET<T>(string cloudURL) : ComponentBase where T : DBBase, IDBBase
    {
        [Inject]
        protected IDexieNETService<T>? DexieNETService { get; set; }

        [NotNull] // OnInitializedAsync will throw
        public T? Dexie { get; private set; }

        protected async override Task OnInitializedAsync()
        {
            if (DexieNETService is not null)
            {
                DexieCloudOptions cloudOptions = new(cloudURL);

                Dexie = await DexieNETService.DexieNETFactory.Create(true);

                if (Dexie is null)
                {
                    throw new InvalidOperationException("Can not create database.");
                }

                Dexie.ConfigureCloud(cloudOptions);
            }

            await base.OnInitializedAsync();
        }
    }
}
