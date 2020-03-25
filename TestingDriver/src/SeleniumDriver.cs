﻿// <copyright file="SeleniumDriver.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace TestingDriver
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Management;
    using System.Reflection;
    using AxeAccessibilityDriver;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Chrome;
    using OpenQA.Selenium.Edge;
    using OpenQA.Selenium.Firefox;
    using OpenQA.Selenium.IE;
    using OpenQA.Selenium.Remote;
    using OpenQA.Selenium.Support.Extensions;
    using OpenQA.Selenium.Support.UI;
    using static TestingDriver.ITestingDriver;
    using static TestingDriver.TestingDriverBuilder;

    /// <summary>
    /// Driver class for Selenium WebDriver.
    /// </summary>
    public class SeleniumDriver : ITestingDriver
    {
        /// <summary>
        /// Location of the Selenium drivers on the current machine.
        /// </summary>
        private readonly string seleniumDriverLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private IAccessibilityChecker axeDriver = null;
        private IWebDriver webDriver;
        private WebDriverWait wdWait;
        private int driverServicePID = -1;

        private string environment;
        private string url;

        private string screenshotSaveLocation;

        private Browser browserType;
        private TimeSpan timeOutThreshold;
        private TimeSpan actualTimeOut;

        private string remoteHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeleniumDriver"/> class.
        /// </summary>
        /// <param name="browser">The browser to use.</param>
        /// <param name="timeOut">The time out in seconds.</param>
        /// <param name="environment">The environment of the test.</param>
        /// <param name="url">Default url to naivgate to.</param>
        /// <param name="screenshotSaveLocation">Location to save screenshots.</param>
        /// <param name="actualTimeout">Time out limit in minutes.</param>
        /// <param name="loadingSpinner">The xpath for any loading spinners.</param>
        /// <param name="errorContainer">The xpath for any error containers.</param>
        /// <param name="remoteHost">The address of the remote host.</param>
        public SeleniumDriver(
            string browser = "chrome",
            int timeOut = 5,
            string environment = "",
            string url = "",
            string screenshotSaveLocation = "./",
            int actualTimeout = 60,
            string loadingSpinner = "",
            string errorContainer = "",
            string remoteHost = "")
        {
            this.browserType = this.GetBrowserType(browser);
            this.timeOutThreshold = TimeSpan.FromSeconds(timeOut);
            this.environment = environment;
            this.url = url;
            this.screenshotSaveLocation = screenshotSaveLocation;
            this.actualTimeOut = TimeSpan.FromMinutes(actualTimeout);
            this.LoadingSpinner = loadingSpinner;
            this.ErrorContainer = errorContainer;
            this.remoteHost = remoteHost;
        }

        /// <inheritdoc/>
        public TestingDriverType Name { get; } = TestingDriverType.Selenium;

        /// <inheritdoc/>
        public string CurrentURL { get => this.webDriver.Url; }

        /// <inheritdoc/>
        public string LoadingSpinner { get; set; }

        /// <inheritdoc/>
        public string ErrorContainer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the web driver is in an IFrame or not.
        /// </summary>
        private bool InIFrame { get; set; } = false;

        private string IFrameXPath { get; set; } = string.Empty;

        private int CurrentWindow { get; set; } = -1;

        /// <summary>
        /// Checks for an element state.
        /// </summary>
        /// <param name="xPath"> The xpath to find the web element. </param>
        /// <param name="state"> The state of the web element to wait for. </param>
        /// <returns> If the element state is as wanted.</returns>
        public bool CheckForElementState(string xPath, ElementState state)
        {
            IWebElement element = null;

            try
            {
                element = this.GetElementByXPath(xPath, 3);
            }
            catch (NoSuchElementException)
            {
                // this is expected if we are checking that it is not visible.
            }
            catch (WebDriverTimeoutException)
            {
                // this is expected if we are checking that it is not visible.
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }

            switch (state)
            {
                case ElementState.Invisible:
                    return element == null;

                case ElementState.Visible:
                    return element != null && element.Displayed;

                case ElementState.Clickable:
                    return element != null && element.Displayed && element.Enabled;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Performs the actions of clicking the specified element. Uses Selenium binding by default.
        /// </summary>
        /// <param name="xPath">The xpath to find the specified element.</param>
        /// <param name="byJS"> Whether to use JS to perform the click / not. </param>
        public void ClickElement(string xPath, bool byJS = false)
        {
            IWebElement element = this.GetElementByXPath(xPath);
            this.wdWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(element));
            if (byJS)
            {
                IJavaScriptExecutor executor = (IJavaScriptExecutor)this.webDriver;
                executor.ExecuteScript("var element=arguments[0]; setTimeout(function() {element.click();}, 100)", element);
            }
            else
            {
                element.Click();
            }
        }

        /// <summary>
        /// Closes the current window. It will quit the browser if it is the last window opened.
        /// </summary>
        public void CloseBrowser()
        {
            this.webDriver.Close();
        }

        /// <summary>
        /// Quits the webdriver. Call this when you want the driver to be closed.
        /// </summary>
        public void Quit()
        {
            try
            {
                this.webDriver.Quit();
                this.webDriver.Dispose();
            }
            catch
            {
            }
            finally
            {
                try
                {
                    if (this.driverServicePID != -1)
                    {
                        var driverProcessIds = new List<int> { this.driverServicePID };

                        var mos = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessID={this.driverServicePID}");
                        foreach (var mo in mos.Get())
                        {
                            var pid = Convert.ToInt32(mo["ProcessID"]);
                            driverProcessIds.Add(pid);
                        }

                        // Kill all
                        foreach (var id in driverProcessIds)
                        {
                            Process.GetProcessById(id).Kill();
                            Logger.Info($"We just tried killing process {id}");
                        }

                        this.driverServicePID = -1;
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Maximizes the browser.
        /// </summary>
        public void Maximize()
        {
            this.webDriver.Manage().Window.Maximize();
        }

        /// <summary>
        /// Generates the AODA results.
        /// </summary>
        /// <param name="folderLocation"> The folder to generate AODA results in. </param>
        public void GenerateAODAResults(string folderLocation)
        {
            this.axeDriver.LogResults(folderLocation);
        }

        /// <summary>
        /// Tells the browser to navigate to the provided url.
        /// </summary>
        /// <param name="url">URL for the browser to navigate to.</param>
        /// <param name="instantiateNewDriver">Instantiates a new selenium driver.</param>
        /// <returns> <code>true</code> if the navigation was successful. </returns>
        public bool NavigateToURL(string url = "", bool instantiateNewDriver = true)
        {
            try
            {
                if (url == string.Empty)
                {
                    url = this.url;
                }

                if (instantiateNewDriver)
                {
                    this.InstantiateSeleniumDriver();
                }

                this.webDriver.Url = url;
                return true;
            }
            catch (Exception e)
            {
                Logger.Error($"Something went wrong while navigating to url: {e.ToString()}");
                return false;
            }
        }

        /// <summary>
        /// Performs the action of populating a value.
        /// </summary>
        /// <param name="xPath"> The xpath to use to identify the element. </param>
        /// <param name="value"> The value to populate.</param>
        public void PopulateElement(string xPath, string value)
        {
            IWebElement element = this.GetElementByXPath(xPath);
            this.wdWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(element));
            element.Click();
            element.Clear();
            element.SendKeys(value);
        }

        /// <summary>
        /// Refreshes the webpage.
        /// </summary>
        public void RefreshWebPage()
        {
            this.webDriver.Navigate().Refresh();
        }

        /// <summary>
        /// Method to run aoda on the current web page.
        /// </summary>
        /// <param name="providedPageTitle"> Title of the web page the user provides. </param>
        public void RunAODA(string providedPageTitle)
        {
            try
            {
                this.axeDriver.CaptureResult(providedPageTitle);
            }
            catch (Exception e)
            {
                Logger.Error($"Axe Driver failed to capture results. Stack trace: {e}");
            }
        }

        /// <summary>
        /// Performs the action of selecting a value in an element.
        /// </summary>
        /// <param name="xPath"> The xpath to use to identify the element. </param>
        /// <param name="value"> The value to select in the element.</param>
        public void SelectValueInElement(string xPath, string value)
        {
            IWebElement ddlElement = this.GetElementByXPath(xPath);
            SelectElement ddl = new SelectElement(ddlElement);
            this.wdWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(ddlElement));
            ddl.SelectByText(value);
        }

        /// <summary>
        /// Switches to appropriate IFrame.
        /// </summary>
        /// <param name="xPath"> xPath to find the iFrame.</param>
        public void SwitchToIFrame(string xPath)
        {
            this.SetActiveTab();
            this.webDriver.SwitchTo().DefaultContent();

            if (xPath == "root")
            {
                this.InIFrame = false;
            }
            else
            {
                this.wdWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.FrameToBeAvailableAndSwitchToIt(By.XPath(xPath)));
                this.InIFrame = true;
                this.IFrameXPath = xPath;
            }
        }

        /// <summary>
        /// The SwitchToTab.
        /// </summary>
        /// <param name="tab">The tab<see cref="int"/>.</param>
        public void SwitchToTab(int tab)
        {
            var tabs = this.webDriver.WindowHandles;
            this.webDriver.SwitchTo().Window(tabs[tab]);
        }

        /// <summary>
        /// Takes a screenshot of the browser. Screenshot will have the datestamp as its name. Year Month Date Hour Minutes Seconds (AM/PM).
        /// </summary>
        public void TakeScreenShot()
        {
            try
            {
                Screenshot screenshot = this.webDriver.TakeScreenshot();
                screenshot.SaveAsFile(this.screenshotSaveLocation + "\\" + $"{DateTime.Now:yyyy_MM_dd-hh_mm_ss_tt}.png");
            }
            catch
            {
            }
        }

        /// <summary>
        /// Waits for an element state.
        /// </summary>
        /// <param name="xPath"> The xpath to find the web element. </param>
        /// <param name="state"> The state of the web element to wait for. </param>
        public void WaitForElementState(string xPath, ElementState state)
        {
            switch (state)
            {
                case ElementState.Invisible:

                    this.wdWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.InvisibilityOfElementLocated(By.XPath(xPath)));
                    break;

                case ElementState.Visible:
                    this.wdWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.XPath(xPath)));
                    break;

                case ElementState.Clickable:
                    this.wdWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.XPath(xPath)));
                    break;
            }
        }

        /// <summary>
        /// Waits until the loading spinner disappears.
        /// </summary>
        public void WaitForLoadingSpinner()
        {
            try
            {
                this.SetActiveTab();

                if (this.LoadingSpinner != string.Empty)
                {
                    this.wdWait.Until(
                        SeleniumExtras.WaitHelpers.ExpectedConditions.InvisibilityOfElementLocated(
                            By.XPath(this.LoadingSpinner)));
                }
            }
            catch (Exception)
            {
                // we want to do nothing here
            }
        }

        /// <summary>
        /// Checks if there are any errors in the error container.
        /// </summary>
        public void CheckErrorContainer()
        {
            if (this.ErrorContainer != string.Empty)
            {
                try
                {
                    IWebElement errorContainer = this.webDriver.FindElement(By.XPath(this.ErrorContainer));
                    Logger.Error($"Found the following in the error container: {errorContainer.Text}");
                }
                catch (Exception)
                {
                    // we do nothing if we don't find it.
                }
            }
        }

        private Browser GetBrowserType(string browserName)
        {
            Browser browser;
            if (browserName.ToLower().Contains("chrome"))
            {
                if (browserName.ToLower().Contains("remote"))
                {
                    browser = Browser.RemoteChrome;
                }
                else
                {
                    browser = Browser.Chrome;
                }
            }
            else if (browserName.ToLower().Contains("ie"))
            {
                browser = Browser.IE;
            }
            else if (browserName.ToLower().Contains("firefox"))
            {
                browser = Browser.Firefox;
            }
            else if (browserName.ToLower().Contains("edge"))
            {
                browser = Browser.Edge;
            }
            else
            {
                Logger.Error($"Sorry we do not currently support the browser: {browserName}");
                throw new Exception("Unsupported Browser.");
            }

            return browser;
        }

        /// <summary>
        /// Finds the first IWebElement By XPath.
        /// </summary>
        /// <param name="xPath">The xpath to find the web element.</param>
        /// <returns> The first IWebElement whose xpath matches. </returns>
        private IWebElement GetElementByXPath(string xPath)
        {
            this.WaitForLoadingSpinner();
            return this.wdWait.Until(driver => driver.FindElement(By.XPath(xPath)));
        }

        /// <summary>
        /// Finds the first IWebElement By XPath.
        /// </summary>
        /// <param name="xPath">The xpath to find the web element.</param>
        /// <param name="tries"> The amount in seconds to wait for.</param>
        /// <returns> The first IWebElement whose xpath matches. </returns>
        private IWebElement GetElementByXPath(string xPath, int tries)
        {
            this.WaitForLoadingSpinner();
            IWebElement element = null;
            for (int i = 0; i < tries; i++)
            {
                try
                {
                    element = this.webDriver.FindElement(By.XPath(xPath));
                    break;
                }
                catch
                {
                }
            }

            return element;
        }

        private void InstantiateSeleniumDriver()
        {
            try
            {
                this.Quit();

                this.webDriver = null;

                ChromeOptions chromeOptions;
                ChromeDriverService service;

                switch (this.browserType)
                {
                    case Browser.RemoteChrome:

                        chromeOptions = new ChromeOptions
                        {
                            UnhandledPromptBehavior = UnhandledPromptBehavior.Accept,
                        };

                        chromeOptions.AddArgument("no-sandbox");
                        chromeOptions.AddArgument("--log-level=3");
                        chromeOptions.AddArgument("--silent");

                        this.webDriver = new RemoteWebDriver(new Uri(this.remoteHost), chromeOptions.ToCapabilities(), this.actualTimeOut);

                        break;
                    case Browser.Chrome:

                        chromeOptions = new ChromeOptions
                        {
                            UnhandledPromptBehavior = UnhandledPromptBehavior.Accept,
                        };

                        chromeOptions.AddArgument("no-sandbox");
                        chromeOptions.AddArgument("--log-level=3");
                        chromeOptions.AddArgument("--silent");
                        chromeOptions.BinaryLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\chromium\\chrome.exe";

                        service = ChromeDriverService.CreateDefaultService(this.seleniumDriverLocation);
                        service.SuppressInitialDiagnosticInformation = true;

                        this.webDriver = new ChromeDriver(this.seleniumDriverLocation, chromeOptions, this.actualTimeOut);
                        this.driverServicePID = service.ProcessId;
                        Logger.Info($"Chrome Driver service PID is: {this.driverServicePID}");

                        break;
                    case Browser.Edge:

                        this.webDriver = new EdgeDriver(this.seleniumDriverLocation, null, this.actualTimeOut);

                        break;
                    case Browser.Firefox:

                        this.webDriver = new FirefoxDriver(this.seleniumDriverLocation, null, this.actualTimeOut);

                        break;
                    case Browser.IE:

                        InternetExplorerOptions ieOptions = new InternetExplorerOptions
                        {
                            IntroduceInstabilityByIgnoringProtectedModeSettings = true,
                            IgnoreZoomLevel = true,
                            EnsureCleanSession = true,
                            EnableNativeEvents = bool.Parse(ConfigurationManager.AppSettings["IEEnableNativeEvents"].ToString()),
                            UnhandledPromptBehavior = UnhandledPromptBehavior.Accept,
                            RequireWindowFocus = true,
                        };
                        InternetExplorerDriverService ieService = InternetExplorerDriverService.CreateDefaultService(this.seleniumDriverLocation);
                        ieService.SuppressInitialDiagnosticInformation = true;
                        this.webDriver = new InternetExplorerDriver(ieService, ieOptions, this.actualTimeOut);

                        this.driverServicePID = ieService.ProcessId;
                        Logger.Info($"Internet Driver service PID is: {this.driverServicePID}");

                        break;
                    case Browser.Safari:

                        Logger.Info("We currently do not deal with Safari yet!");

                        break;
                }

                this.wdWait = new WebDriverWait(this.webDriver, this.timeOutThreshold);

                if (this.axeDriver == null)
                {
                    this.axeDriver = new AxeDriver(this.webDriver);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"While trying to instantiate Selenium drivers, we were met with the following: {e.ToString()}");
                this.Quit();
            }
        }

        /// <summary>
        /// Sets the tab to be the active one.
        /// </summary>
        private void SetActiveTab()
        {
            if (!this.InIFrame)
            {
                var windows = this.webDriver.WindowHandles;
                int windowCount = windows.Count;

                // save the current window / tab we are on. Only focus the browser when a new page / tab actually is there.
                if (windowCount != this.CurrentWindow)
                {
                    this.CurrentWindow = windowCount;
                    this.webDriver.SwitchTo().Window(windows[windowCount - 1]);
                }
            }
            else
            {
                this.wdWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.FrameToBeAvailableAndSwitchToIt(By.XPath(this.IFrameXPath)));
            }
        }
    }
}
