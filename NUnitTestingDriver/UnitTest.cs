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
            driver = new SeleniumDriver();
        }

        [Test]
        public void Test1()
        {
            driver.NavigateToURL("http://the-internet.herokuapp.com/");
            driver.ClickElement("//a[contains(text(),'Form Authentication')]");
            Thread.Sleep(5000);
        }

        [TearDown]
        public void TearDown()
        {
            driver.Quit();
        }
    }
}