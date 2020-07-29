using NUnit.Framework;
using System;
using System.Collections.Generic;
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
            driver = new SeleniumDriver(browser: "Chrome", timeOut: 5);
#else
            driver = new SeleniumDriver(browser:"remoteChrome", remoteHost:"http://localhost:4444/wd/hub");
#endif
            driver.NavigateToURL("http://the-internet.herokuapp.com/");
        }

        [Test]
        public void TestBackAndForwards()
        {
            driver.ClickElement("//a[contains(text(),'Dropdown')]");
            driver.Back();
            driver.Forward();
            driver.SelectValueInElement("//select[@id='dropdown']", "Option 1");
            Assert.Pass("Nothing should go wrong when clicking a drop down");
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
        public void TestCheckElementState()
        {
            driver.ClickElement("//a[contains(text(),'Dynamic Controls')]");
            Assert.IsTrue(driver.CheckForElementState("//div[@id='checkbox']//input", ITestingDriver.ElementState.Clickable),"Element should be clickable");
            Assert.IsTrue(driver.CheckForElementState("//div[@id='checkbox']//input", ITestingDriver.ElementState.Visible), "Element should be visible");
            Assert.IsFalse(driver.CheckForElementState("//div[@id='checkbox']//input", ITestingDriver.ElementState.Invisible), "Element should be clickable");
            Assert.IsFalse(driver.CheckForElementState("//div[@id='checkbox']//input", ITestingDriver.ElementState.Disabled), "Element is not be disabled");
            Assert.IsTrue(driver.CheckForElementState("//div[@id='plkj']", ITestingDriver.ElementState.Invisible), "Element should be invisible");
            Assert.IsFalse(driver.CheckForElementState("//div[@id='plkj']", ITestingDriver.ElementState.Visible), "Element should be invisible");
            Assert.IsFalse(driver.CheckForElementState("//div[@id='plkj']", ITestingDriver.ElementState.Clickable), "Element should be invisible");
            Assert.IsFalse(driver.CheckForElementState("//div[@id='plkj']", ITestingDriver.ElementState.Disabled), "Element is not be disabled");
            Assert.IsTrue(driver.CheckForElementState("//form[@id='input-example']//input", ITestingDriver.ElementState.Disabled), "Element should be Disabled");
            Assert.IsFalse(driver.CheckForElementState("//form[@id='input-example']//input", ITestingDriver.ElementState.Clickable), "Element is not Clickable");
            Assert.IsTrue(driver.CheckForElementState("//form[@id='input-example']//input", ITestingDriver.ElementState.Visible), "Element is Visible");
            Assert.IsFalse(driver.CheckForElementState("//form[@id='input-example']//input", ITestingDriver.ElementState.Invisible), "Element is not Invisible");
        }

        [Test]
        public void TestIFrames()
        {
            driver.ClickElement("//a[contains(text(),'WYSIWYG Editor')]");
            driver.SwitchToIFrame("//iframe[@id='mce_0_ifr']");
            Assert.IsTrue(driver.CheckForElementState("//body", ITestingDriver.ElementState.Clickable),"element should be clickable");
            Assert.IsTrue(driver.VerifyElementText("Your content goes here.", "//body"), "The text should be 'Your content goes here.'");
            driver.PopulateElement("//body", "Hello World");
            driver.SwitchToIFrame("root");
            driver.SwitchToIFrame("//iframe[@id='mce_0_ifr']");
            Assert.IsTrue(driver.VerifyElementText("Hello World", "//body"), "The new text should be 'Hello World'");
        }

        [Test]
        public void TestVerifyAttribute()
        {
            driver.ClickElement("//a[contains(text(),'Broken Images')]");
            Assert.IsTrue(driver.VerifyAttribute("src", "http://the-internet.herokuapp.com/img/avatar-blank.jpg", "//body//img[3]"), "The attribute src should equal http://the-internet.herokuapp.com/img/avatar-blank.jpg");
            Assert.IsFalse(driver.VerifyAttribute("src", "img/image.jpg", "//body//img[3]"), "The attribute src should not equal img/image.jpg");
        }

        [Test]
        public void TestVerifyElementText()
        {
            Assert.IsTrue(driver.VerifyElementText("Available Examples","//h2[contains(text(),'Available Examples')]"),"This should be the text");
            Assert.IsFalse(driver.VerifyElementText("Available", "//h2[contains(text(),'Available Examples')]"),"This is the wrong text");
        }

        [Test]
        public void TestVerifyElementSelected()
        {
            driver.ClickElement("//a[contains(text(),'Checkboxes')]");
            Assert.IsTrue(driver.VerifyElementSelected("//body//input[2]"),"This element is selected");
            Assert.IsFalse(driver.VerifyElementSelected("//body//input[1]"),"This element is not selected");
        }

        [Test]
        public void TestBadXPath()
        {
            try
            {
                driver.ClickElement("//a[]");
                Assert.Fail("This should fail");
            }
            catch (Exception)
            {
                Assert.Pass();
            }
        }


        [Test]
        public void TestVerifyDropDownContent()
        {
            driver.ClickElement("//a[contains(text(),'Dropdown')]");
            Assert.IsTrue(driver.VerifyDropDownContent(new List<string>() {"Option 1","Option 2" }, "//select[@id='dropdown']"));
            Assert.IsFalse(driver.VerifyDropDownContent(new List<string>() { "Option 1", "Option 3" }, "//select[@id='dropdown']"));
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