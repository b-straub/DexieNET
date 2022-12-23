using DexieNETTest.Tests.Infrastructure;
using Xunit;

namespace DexieNETTest.Tests.Tests
{
    [CollectionDefinition("Chromium", DisableParallelization = true)]
    public class ChromiumCollection : ICollectionFixture<ChromiumFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }


    [Collection("Chromium")]
    public class ChromiumTest : DexieNETTestBase
    {
        public ChromiumTest(ChromiumFixture fixture) : base(fixture) { }
    }

#if DEBUG
    [CollectionDefinition("Firefox", DisableParallelization = true)]
    public class FirefoxCollection : ICollectionFixture<FirefoxFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    [Collection("Firefox")]
    public class FirefoxTest : DexieNETTestBase
    {
        public FirefoxTest(FirefoxFixture fixture) : base(fixture) { }
    }

#if !Windows
    [CollectionDefinition("Webkit", DisableParallelization = true)]
    public class WebkitCollection : ICollectionFixture<WebkitFixture>
    {
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
    }

    [Collection("Webkit")]
    public class WebkitTest : DexieNETTestBase
    {
    public WebkitTest(WebkitFixture fixture) : base(fixture) { }
    }
#endif
#endif
}
