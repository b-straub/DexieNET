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

        private readonly string? _cloudURL;

        public DexieNET(string? cloudURL = null)
        {
            _cloudURL = cloudURL;
        }   

        protected async override Task OnInitializedAsync()
        {
            if (DexieNETService is not null)
            {
                DexieCloudOptions? cloudOptions = null;

				if (_cloudURL is not null)
				{
					cloudOptions = new(_cloudURL);
				}

				Dexie = await DexieNETService.DexieNETFactory.Create(cloudOptions is not null);

                if (Dexie is null)
                {
                    throw new InvalidOperationException("Can not create database.");
                }

                if (cloudOptions is not null)
                {
                    Dexie.ConfigureCloud(cloudOptions);
                }
            }

            await base.OnInitializedAsync();
        }
    }
}
