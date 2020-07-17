// <copyright file="SeleniumDriver.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace TestingDriver
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Management;
    using System.Reflection;
    using AxeAccessibilityDriver;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Chrome;
    using OpenQA.Selenium.Edge;
    using OpenQA.Selenium.Firefox;
    using OpenQA.Selenium.IE;
    using OpenQA.Selenium.Interactions;
    using OpenQA.Selenium.Remote;
    using OpenQA.Selenium.Support.Extensions;
    using OpenQA.Selenium.Support.UI;
    using static TestingDriver.ITestingDriver;

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
        private WebDriverWait wdWait;
        private Actions Action;

        private int PID = -1;

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
        /// <param name="webDriver">Any Web driver to be passed in.</param>
        public SeleniumDriver(
            string browser = "chrome",
            int timeOut = 5,
            string environment = "",
            string url = "",
            string screenshotSaveLocation = "./",
            int actualTimeout = 60,
            string loadingSpinner = "",
            string errorContainer = "",
            string remoteHost = "",
            IWebDriver webDriver = null)
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
            this.WebDriver = webDriver;
        }

        /// <inheritdoc/>
        public TestingDriverType Name { get; } = TestingDriverType.Selenium;

        /// <inheritdoc/>
        public string CurrentURL { get => this.WebDriver.Url; }

        /// <inheritdoc/>
        public string LoadingSpinner { get; set; }

        /// <inheritdoc/>
        public string ErrorContainer { get; set; }

        /// <summary>
        /// Gets the web driver in use.
        /// </summary>
        public IWebDriver WebDriver { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the web driver is in an IFrame or not.
        /// </summary>
        private bool InIFrame { get; set; } = false;

        private string IFrameXPath { get; set; } = string.Empty;

        private int CurrentWindow { get; set; } = -1;

        /// <summary>
        /// Returns the webElement at the given xPath.
        /// </summary>
        /// <param name="xPath">The xpath to find the element at.</param>
        /// <param name="jsCommand">any Js Command To use.</param>
        /// <returns>The web element.</returns>
        public IWebElement GetWebElement(string xPath, string jsCommand = "")
        {
           return this.FindElement(xPath, jsCommand);
        }

        /// <inheritdoc/>
        public void AcceptAlert()
        {
            this.WebDriver.SwitchTo().Alert().Accept();
            this.SetActiveTab();
        }

        /// <inheritdoc/>
        public void Back()
        {
            this.WebDriver.Navigate().Back();
        }

        /// <inheritdoc/>
        public bool CheckForElementState(string xPath, ElementState state, string jsCommand = "")
        {
            IWebElement element = null;

            try
            {
                element = this.FindElement(xPath, jsCommand, 3);
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
                    if (element != null)
                    {
                        bool isReadOnly = bool.Parse(element.GetAttribute("readonly") ?? "false");
                        return element.Displayed && element.Enabled && !isReadOnly;
                    }
                    else
                    {
                        return false;
                    }

                default:
                    return false;
            }
        }

        /// <inheritdoc/>
        public void ClickElement(string xPath, bool byJS = false, string jsCommand = "")
        {
            IWebElement element = this.FindElement(xPath, jsCommand);
            this.wdWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(element));
            if (byJS)
            {
                IJavaScriptExecutor executor = (IJavaScriptExecutor)this.WebDriver;
                executor.ExecuteScript("var element=arguments[0]; setTimeout(function() {element.click();}, 100)", element);
            }
            else
            {
                element.Click();
            }
        }

        /// <inheritdoc/>
        public void CloseBrowser()
        {
            this.WebDriver.Close();
        }

        /// <inheritdoc/>
        public void DismissAlert()
        {
            this.WebDriver.SwitchTo().Alert().Dismiss();
            this.SetActiveTab();
        }

        /// <inheritdoc/>
        public void Forward()
        {
            this.WebDriver.Navigate().Forward();
        }

        /// <inheritdoc/>
        public string GetElementAttribute(string attribute, string xPath, string jsCommand = "")
        {
            IWebElement element = this.FindElement(xPath, jsCommand);
            return element.GetAttribute(attribute);
        }

        /// <inheritdoc/>
        public string GetElementText(string xPath, string jsCommand = "")
        {
            IWebElement element = this.FindElement(xPath, jsCommand);
            return element.Text;
        }

        /// <inheritdoc/>
        public string GetAlertText()
        {
            return this.WebDriver.SwitchTo().Alert().Text;
        }

        /// <inheritdoc/>
        public void Quit()
        {
            try
            {
                if (this.WebDriver != null)
                {
                    this.WebDriver.Quit();
                    this.WebDriver.Dispose();
                }
            }
            catch
            {
            }
            finally
            {
                this.ForceKillWebDriver();
            }
        }

        /// <inheritdoc/>
        public void Maximize()
        {
            this.WebDriver.Manage().Window.Maximize();
        }

        /// <inheritdoc/>
        public void ForceKillWebDriver()
        {
            try
            {
                if (this.PID != -1)
                {
                    var driverProcessIds = new List<int> { this.PID };

                    var mos = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessID={this.PID}");
                    foreach (var mo in mos.Get())
                    {
                        var pid = Convert.ToInt32(mo["ProcessID"]);
                        driverProcessIds.Add(pid);
                    }

                    // Kill all
                    foreach (var id in driverProcessIds)
                    {
                        Process.GetProcessById(id).Kill();
                        Console.WriteLine($"We just tried killing process {id}");
                    }

                    this.PID = -1;
                }
            }
            catch
            {
            }
            finally
            {
                this.PID = -1;
            }
        }

        /// <inheritdoc/>
        public void GenerateAODAResults(string folderLocation)
        {
            this.axeDriver.LogResults(folderLocation);
        }

        /// <inheritdoc/>
        public List<string> GetAllLinksURL()
        {
            this.WaitForLoadingSpinner();
            var allElements = this.WebDriver.FindElements(By.TagName("a"));
            List<string> result = new List<string>();
            foreach (IWebElement link in allElements)
            {
                string url = link.GetAttribute("href");
                if (!url.Contains("javascript"))
                {
                    result.Add(url);
                }
            }

            return result;
        }

        /// <inheritdoc/>
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

                this.WebDriver.Url = url;
                return true;
            }
            catch (Exception e)
            {
                Logger.Error($"Something went wrong while navigating to url: {e.ToString()}");
                return false;
            }
        }

        /// <inheritdoc/>
        public void PopulateElement(string xPath, string value, string jsCommand = "")
        {
            IWebElement element = this.FindElement(xPath, jsCommand);
            this.wdWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(element));
            element.Click();
            element.Clear();
            element.SendKeys(value);
        }

        /// <inheritdoc/>
        public void RefreshWebPage()
        {
            this.WebDriver.Navigate().Refresh();
        }

        /// <inheritdoc/>
        public void RunAODA(string providedPageTitle)
        {
            try
            {
                this.WebDriver.SwitchTo().DefaultContent();
                this.axeDriver.CaptureResult(providedPageTitle);
                if (this.InIFrame)
                {
                    this.SwitchToIFrame(this.IFrameXPath, string.Empty);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Axe Driver failed to capture results. Stack trace: {e}");
            }
        }

        /// <inheritdoc/>
        public void SendKeys(string keystroke)
        {
            Actions action = new Actions(this.WebDriver);
            if (keystroke == "{ENTER}")
            {
                action.SendKeys(Keys.Enter);
            }
            else if (keystroke == "{TAB}")
            {
                action.SendKeys(Keys.Tab);
            }
            else
            {
                action.SendKeys(keystroke);
            }
        }

        /// <inheritdoc/>
        public void SelectValueInElement(string xPath, string value, string jsCommand)
        {
            IWebElement ddlElement = this.FindElement(xPath, jsCommand);
            SelectElement ddl = new SelectElement(ddlElement);
            this.wdWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(ddlElement));
            ddl.SelectByText(value);
        }

        /// <inheritdoc/>
        public void SetTimeOutThreshold(string seconds)
        {
            this.wdWait = new WebDriverWait(this.WebDriver, TimeSpan.FromSeconds(Convert.ToDouble(seconds)));
        }

        /// <inheritdoc/>
        public void SwitchToIFrame(string xPath, string jsCommand)
        {
            this.SetActiveTab();
            this.WebDriver.SwitchTo().DefaultContent();

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

        /// <inheritdoc/>
        public void SwitchToTab(int tab)
        {
            var tabs = this.WebDriver.WindowHandles;
            this.WebDriver.SwitchTo().Window(tabs[tab]);
        }

        /// <inheritdoc/>
        public void TakeScreenShot()
        {
            try
            {
                Screenshot screenshot = this.WebDriver.TakeScreenshot();
                screenshot.SaveAsFile(this.screenshotSaveLocation + "\\" + $"{DateTime.Now:yyyy_MM_dd-hh_mm_ss_tt}.png");
            }
            catch
            {
            }
        }

        /// <inheritdoc/>
        public void WaitForElementState(string xPath, ElementState state, string jsCommand = "")
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void Wait(int seconds)
        {
            this.WebDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(seconds);
        }

        /// <inheritdoc/>
        public bool VerifyAttribute(string attribute, string expectedValue, string xPath, string jsCommand = "")
        {
            IWebElement element = this.FindElement(xPath, jsCommand);
            attribute = attribute.ToLower();
            return element.GetAttribute(attribute) == expectedValue;
        }

        /// <inheritdoc/>
        public bool VerifyElementText(string expected, string xPath, string jsCommand = "")
        {
            IWebElement element = this.FindElement(xPath, jsCommand);
            bool result = expected == element.Text;
            return result;
        }

        /// <inheritdoc/>
        public bool VerifyElementSelected(string xPath, string jsCommand = "")
        {
            IWebElement element = this.FindElement(xPath, jsCommand);
            return element.Selected;
        }

        /// <inheritdoc/>
        public bool VerifyDropDownContent(List<string> expected, string xPath, string jsCommand = "")
        {
            IWebElement element = this.FindElement(xPath, jsCommand);
            SelectElement selectElement = new SelectElement(element);

            List<string> actualValue = new List<string>();
            foreach (IWebElement e in selectElement.Options)
            {
                actualValue.Add(e.Text);
            }

            return expected.All(e => actualValue.Contains(e));
        }

        /// <inheritdoc/>
        public void CheckErrorContainer()
        {
            if (this.ErrorContainer != string.Empty)
            {
                try
                {
                    IWebElement errorContainer = this.WebDriver.FindElement(By.XPath(this.ErrorContainer));
                    Logger.Error($"Found the following in the error container: {errorContainer.Text}");
                }
                catch (Exception)
                {
                    // we do nothing if we don't find it.
                }
            }
        }

        /// <inheritdoc/>
        public void ExecuteJS(string jsCommand)
        {
            ((IJavaScriptExecutor)this.WebDriver).ExecuteScript(jsCommand);
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
                    element = this.WebDriver.FindElement(By.XPath(xPath));
                    break;
                }
                catch
                {
                }
            }

            return element;
        }

        /// <summary>
        /// Executes JS command on this element.
        /// </summary>
        /// <param name="jsCommand">command.</param>
        /// <param name="webElement">Elemnt to interact with.</param>
        private void ExecuteJS(string jsCommand, IWebElement webElement)
        {
            ((IJavaScriptExecutor)this.WebDriver).ExecuteScript(jsCommand, webElement);
        }

        /// <summary>
        /// Moves the mouse to the given element.
        /// </summary>
        /// <param name="element">Web element to mouse over.</param>
        private void MouseOver(IWebElement element)
        {
            this.Action.MoveToElement(element).Build().Perform();
        }

        /// <summary>
        /// The FindElementByJs.
        /// </summary>
        /// <param name="jsCommand">The jsCommand<see cref="string"/>.</param>
        /// <param name="webElements">The webElements<see cref="List{IWebElement}"/>.</param>
        /// <returns>The <see cref="IWebElement"/>.</returns>
        private IWebElement FindElementByJs(string jsCommand, List<IWebElement> webElements)
        {
            this.SetActiveTab();
            var element = ((IJavaScriptExecutor)this.WebDriver).ExecuteScript(jsCommand, webElements);
            return (IWebElement)element;
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
        /// Finds the web element of the corresponding test object under the given timeout duration and trys.
        /// </summary>
        /// <param name="xPath">The xPath of the element.</param>.
        /// <param name="jsCommand">Optional. Any java script commands to use.</param>
        /// <param name="trys">Optional. Number of trys before giving up.</param>
        /// <returns>The web element of the corresponding test object.</returns>
        private IWebElement FindElement(string xPath, string jsCommand = "", int trys = -1)
        {
            IWebElement webElement = null;
            double timeout = this.timeOutThreshold.TotalSeconds;
            bool errorThrown = false;

            // wait for browser to finish loading before finding the object
            this.WaitForLoadingSpinner();

            // wait for timeout or until object is found
            var stopWatch = Stopwatch.StartNew();
            stopWatch.Start();
            var start = stopWatch.Elapsed.TotalSeconds;

            while ((stopWatch.Elapsed.TotalSeconds - start) < timeout && webElement == null && trys != 0)
            {
                try
                {
                    List<IWebElement> webElements = this.WebDriver.FindElements(By.XPath(xPath)).ToList();
                    if (jsCommand != string.Empty)
                    {
                        webElement = (IWebElement)((IJavaScriptExecutor)this.WebDriver).ExecuteScript(jsCommand, webElements);
                    }
                    else
                    {
                        if (webElements.Count > 0)
                        {
                            webElement = webElements[0];
                        }
                    }
                }
                catch (StaleElementReferenceException)
                {
                    // do nothing, since we didn't find the element.
                }
                catch (Exception e)
                {
                    if (!errorThrown)
                    {
                        errorThrown = true;
                        Logger.Error(e.ToString());
                    }
                }

                if (trys > 0)
                {
                    trys--;
                }
            }

            stopWatch.Stop();
            return webElement;
        }

        private void InstantiateSeleniumDriver()
        {
            try
            {
                this.Quit();

                this.WebDriver = null;

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

                        this.WebDriver = new RemoteWebDriver(new Uri(this.remoteHost), chromeOptions.ToCapabilities(), this.actualTimeOut);

                        break;
                    case Browser.Chrome:

                        string chromiumFolderLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\chromium";

                        chromeOptions = new ChromeOptions
                        {
                            UnhandledPromptBehavior = UnhandledPromptBehavior.Accept,
                        };

                        chromeOptions.AddArgument("no-sandbox");
                        chromeOptions.AddArgument("--log-level=3");
                        chromeOptions.AddArgument("--silent");
                        chromeOptions.AddUserProfilePreference("download.prompt_for_download", false);
                        chromeOptions.AddUserProfilePreference("download.default_directory", @"C:\Temp");
                        chromeOptions.AddUserProfilePreference("disable-popup-blocking", true);
                        chromeOptions.AddUserProfilePreference("plugins.always_open_pdf_externally", true);
                        chromeOptions.BinaryLocation = $"{chromiumFolderLocation}\\chrome.exe";

                        // we want to find all the files under the location of chromium\Extensions and add them in.
                        if (Directory.Exists(chromiumFolderLocation + "\\Extensions"))
                        {
                            foreach (string extension in Directory.GetFiles(chromiumFolderLocation + "\\Extensions"))
                            {
                                chromeOptions.AddExtension(extension);
                            }
                        }

                        service = ChromeDriverService.CreateDefaultService(this.seleniumDriverLocation);
                        service.SuppressInitialDiagnosticInformation = true;

                        this.WebDriver = new ChromeDriver(this.seleniumDriverLocation, chromeOptions, this.actualTimeOut);
                        this.PID = service.ProcessId;
                        Logger.Info($"Chrome Driver service PID is: {this.PID}");

                        break;
                    case Browser.Edge:

                        // this.webDriver = new EdgeDriver(this.seleniumDriverLocation, null, this.actualTimeOut);
                        // This is to test Micrsoft Edge (Chromium Based)
                        ChromeOptions options = new ChromeOptions
                        {
                            UnhandledPromptBehavior = UnhandledPromptBehavior.Accept,
                        };
                        options.AddArgument("no-sandbox");
                        options.AddArgument("--log-level=3");
                        options.AddArgument("--silent");
                        options.AddUserProfilePreference("download.prompt_for_download", false);
                        options.AddUserProfilePreference("download.default_directory", @"C:\Temp");
                        options.AddUserProfilePreference("disable-popup-blocking", true);
                        options.AddUserProfilePreference("plugins.always_open_pdf_externally", true);
                        string edgeFolderLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\edge";
                        options.BinaryLocation = edgeFolderLocation + "\\msedge.exe";

                        // we want to find all the files under the location of edge\Extensions and add them in.
                        if (Directory.Exists(edgeFolderLocation + "\\Extensions"))
                        {
                            foreach (string extension in Directory.GetFiles(edgeFolderLocation + "\\Extensions"))
                            {
                                options.AddExtension(extension);
                            }
                        }

                        ChromeDriverService edgeService = ChromeDriverService.CreateDefaultService(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "msedgedriver.exe");

                        edgeService.SuppressInitialDiagnosticInformation = true;
                        this.WebDriver = new ChromeDriver(edgeService, options, this.actualTimeOut);
                        this.PID = edgeService.ProcessId;

                        break;
                    case Browser.Firefox:

                        FirefoxOptions fireFoxOptions = new FirefoxOptions();
                        fireFoxOptions.SetPreference("browser.download.folderList", 2);
                        fireFoxOptions.SetPreference("browser.download.dir", @"C:\Temp");
                        fireFoxOptions.SetPreference("browser.download.manager.alertOnEXEOpen", false);
                        fireFoxOptions.SetPreference("browser.helperApps.neverAsk.saveToDisk", "application/msword, application/csv, application/ris, text/csv, image/png, application/pdf, text/html, text/plain, application/zip, application/x-zip, application/x-zip-compressed, application/download, application/octet-stream");
                        fireFoxOptions.SetPreference("browser.download.manager.showWhenStarting", false);
                        fireFoxOptions.SetPreference("browser.download.manager.focusWhenStarting", false);
                        fireFoxOptions.SetPreference("browser.download.useDownloadDir", true);
                        fireFoxOptions.SetPreference("browser.helperApps.alwaysAsk.force", false);
                        fireFoxOptions.SetPreference("browser.download.manager.alertOnEXEOpen", false);
                        fireFoxOptions.SetPreference("browser.download.manager.closeWhenDone", true);
                        fireFoxOptions.SetPreference("browser.download.manager.showAlertOnComplete", false);
                        fireFoxOptions.SetPreference("browser.download.manager.useWindow", false);
                        fireFoxOptions.SetPreference("services.sync.prefs.sync.browser.download.manager.showWhenStarting", false);
                        fireFoxOptions.SetPreference("pdfjs.disabled", true);

                        string firefoxFolderLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\firefox";
                        fireFoxOptions.BrowserExecutableLocation = firefoxFolderLocation + "\\firefox_.exe";

                        FirefoxDriverService fireFoxService = FirefoxDriverService.CreateDefaultService(this.seleniumDriverLocation);
                        fireFoxService.SuppressInitialDiagnosticInformation = true;

                        this.WebDriver = new FirefoxDriver(fireFoxService, fireFoxOptions, this.actualTimeOut);
                        this.PID = fireFoxService.ProcessId;
                        break;
                    case Browser.IE:

                        // clean session => clear cache and cookies
                        // native events set to true => allow clicking buttons and links when JS is disabled.
                        // Ignore zoom level to be true since having it as the default per resolution has a better result. (ALM #24960)
                        InternetExplorerOptions ieOptions = new InternetExplorerOptions
                        {
                            IntroduceInstabilityByIgnoringProtectedModeSettings = true,
                            IgnoreZoomLevel = true,
                            EnsureCleanSession = true,
                            EnableNativeEvents = bool.Parse(ConfigurationManager.AppSettings["IEEnableNativeEvents"].ToString()),
                            UnhandledPromptBehavior = UnhandledPromptBehavior.Accept,
                            RequireWindowFocus = true,

                            // for the old framwork.
                            EnablePersistentHover = true,
                            PageLoadStrategy = PageLoadStrategy.Normal,
                        };
                        InternetExplorerDriverService ieService = InternetExplorerDriverService.CreateDefaultService(this.seleniumDriverLocation);
                        ieService.SuppressInitialDiagnosticInformation = true;

                        try
                        {
                            this.WebDriver = new InternetExplorerDriver(ieService, ieOptions, this.actualTimeOut);
                            this.PID = ieService.ProcessId;
                            Logger.Info($"Internet Driver service PID is: {this.PID}");
                        }
                        catch (InvalidOperationException ioe)
                        {
                            Logger.Error("Please ensure that protected mode is either all on / off on all zones inside internet options. Exception found was: ");
                            Logger.Error($"{ioe.Message}");
                        }

                        // (ALM #24960) Shortkey to set zoom level to default in IE.
                        IWebElement element = this.WebDriver.FindElement(By.TagName("body"));
                        element.SendKeys(Keys.Control + "0");
                        break;
                    case Browser.Safari:

                        Logger.Info("We currently do not deal with Safari yet!");

                        break;
                    default:
                        Logger.Error("Browser Type is null");
                        break;
                }

                this.wdWait = new WebDriverWait(this.WebDriver, this.timeOutThreshold);
                this.Action = new Actions(this.WebDriver);

                if (this.axeDriver == null)
                {
                    this.axeDriver = new AxeDriver(this.WebDriver);
                }
                else
                {
                    // Make sure to update the driver to the new one.
                    this.axeDriver.Driver = this.WebDriver;
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
                var windows = this.WebDriver.WindowHandles;
                int windowCount = windows.Count;

                // save the current window / tab we are on. Only focus the browser when a new page / tab actually is there.
                if (windowCount != this.CurrentWindow)
                {
                    this.CurrentWindow = windowCount;
                    this.WebDriver.SwitchTo().Window(windows[windowCount - 1]);
                }
            }
            else
            {
                // this.wdWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.FrameToBeAvailableAndSwitchToIt(By.XPath(this.IFrameXPath)));
            }
        }
    }
}
