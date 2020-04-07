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
            driver.NavigateToURL("http://the-internet.herokuapp.com/");
        }

        [Test]
        public void TestLogIn()
        {
            driver.ClickElement("//a[contains(text(),'Form Authentication')]");

            Assert.AreEqual("http://the-internet.herokuapp.com/login", driver.CurrentURL, "URL is incorrect");

            driver.PopulateElement("//input[@id='username']", "tomsmith");
            driver.PopulateElement("//input[@id='password']", "SuperSecretPassword!");
            driver.ClickElement("//button[@class='radius']");

            Assert.AreEqual("http://the-internet.herokuapp.com/secure", driver.CurrentURL, "URL is incorrect");
        }

        [Test]
        public void TestDropDown()
        {
            driver.ClickElement("//a[contains(text(),'Dropdown')]");
            driver.SelectValueInElement("//select[@id='dropdown']", "Option 1");
            Assert.Pass("Nothing should go wrong when clicking a drop down");
        }

        [Test]
        public void TestWaitForElement()
        {
            driver.ClickElement("//a[contains(text(),'Dynamic Controls')]");
            driver.ClickElement("//div[@id='checkbox']//input");
            driver.ClickElement("//button[contains(text(),'Remove')]");
            driver.WaitForElementState("//div[@id='checkbox']//input", ITestingDriver.ElementState.Invisible);
            driver.ClickElement("//button[contains(text(),'Add')]");
            driver.WaitForElementState("//input[@id='checkbox']", ITestingDriver.ElementState.Visible);
            driver.ClickElement("//input[@id='checkbox']");
            Assert.Pass("Nothing should go wrong when going through testing wait for element.");
        }

        [Test]
        public void TestIFrames()
        {
            driver.ClickElement("//li[22]//a[1]");
            driver.ClickElement("//a[contains(text(),'iFrame')]");
            driver.SwitchToIFrame("//iframe[@id='mce_0_ifr']");
            Assert.Pass("Nothing should go wrong when switching into i frames");
        }
#if DEBUG
        [Test]
        public void TestAODA()
        {
            driver.ClickElement("//a[contains(text(),'Form Authentication')]");
            driver.RunAODA("title");
            driver.GenerateAODAResults("./Log");
        }
#endif
        [TearDown]
        public void TearDown()
        {
            driver.Quit();
        }
    }
}