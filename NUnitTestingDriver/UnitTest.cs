using NUnit.Framework;
using System.Threading;
using TestingDriver;
namespace NUnitTestingDriver
{
    public class Tests
    {
        private ITestingDriver driver;

        [SetUp]
        public void Setup()
        {
#if DEBUG
            driver = new SeleniumDriver(browser: "Chrome");
#else
            driver = new SeleniumDriver(browser:"remoteChrome", remoteHost:"http://localhost:4444/wd/hub");
#endif
        }

        [Test]
        public void Test1()
        {
            driver.NavigateToURL("http://the-internet.herokuapp.com/");
            driver.ClickElement("//a[contains(text(),'Form Authentication')]");
            Assert.AreEqual(driver.CurrentURL, "http://the-internet.herokuapp.com/login", "URL is incorrect");
        }

        [TearDown]
        public void TearDown()
        {
            driver.Quit();
        }
    }
}