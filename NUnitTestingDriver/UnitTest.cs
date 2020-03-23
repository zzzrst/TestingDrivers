using NUnit.Framework;
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
            driver.NavigateToURL("https://www.google.com");
        }

        [TearDown]
        public void TearDown()
        {
            driver.Quit();
        }
    }
}