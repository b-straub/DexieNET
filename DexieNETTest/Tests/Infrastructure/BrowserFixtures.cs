using System.Threading.Tasks;

namespace DexieNETTest.Tests.Infrastructure
{
    public class ChromiumFixture : WAFixtureBase, IWAFixture
    {
        public IWAFixture.BrowserType Type => IWAFixture.BrowserType.Chromium;
        public int Port => PortNumber;
        public bool OnePass => true;
        public bool Headless => true;

        private static int PortNumber => 7048;

        public ChromiumFixture() : base(PortNumber) { }

        public async Task InitializeAsync()
        {
            await InitializeAsync(Type, OnePass, Headless);
        }
    }

    public class FirefoxFixture : WAFixtureBase, IWAFixture
    {
        public IWAFixture.BrowserType Type => IWAFixture.BrowserType.Firefox;
        public int Port => PortNumber;
        public bool OnePass => true;
        public bool Headless => true;

        private static int PortNumber => 7049;

        public FirefoxFixture() : base(PortNumber) { }

        public async Task InitializeAsync()
        {
            await InitializeAsync(Type, OnePass, Headless);
        }
    }

    public class WebkitFixture : WAFixtureBase, IWAFixture
    {
        public IWAFixture.BrowserType Type => IWAFixture.BrowserType.Webkit;
        public int Port => PortNumber;
        public bool OnePass => true;
        public bool Headless => false;

        private static int PortNumber => 7050;

        public WebkitFixture() : base(PortNumber) { }

        public async Task InitializeAsync()
        {
            await InitializeAsync(Type, OnePass, Headless);
        }
    }
}
