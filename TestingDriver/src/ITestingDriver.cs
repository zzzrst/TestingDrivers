// <copyright file="ITestingDriver.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace TestingDriver
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using OpenQA.Selenium;

    /// <summary>
    /// The Interface for the Testing Driver software.
    /// </summary>
    public interface ITestingDriver
    {
        /// <summary>
        /// Different browsers that are supported.
        /// </summary>
        public enum Browser
        {
            /// <summary>
            /// Represents the Chrome browser.
            /// </summary>
            Chrome,

            /// <summary>
            /// Represents the Microsoft Edge Browser.
            /// </summary>
            Edge,

            /// <summary>
            /// Represents the Firefox Browser.
            /// </summary>
            Firefox,

            /// <summary>
            /// Represents the Internet Explorer browser.
            /// </summary>
            IE,

            /// <summary>
            /// Represents the Safari Browser.
            /// </summary>
            Safari,

            /// <summary>
            /// Representation of the Chrome in a remote server.
            /// </summary>
            RemoteChrome,
        }

        /// <summary>
        /// Different states of the element.
        /// </summary>
        public enum ElementState
        {
            /// <summary>
            /// Element cannot be found / seen.
            /// </summary>
            Invisible,

            /// <summary>
            /// Element can be seen.
            /// </summary>
            Visible,

            /// <summary>
            /// Element can be clicked.
            /// </summary>
            Clickable,
        }

        /// <summary>
        /// The usable testing applications.
        /// </summary>
        public enum TestingDriverType
        {
            /// <summary>
            /// Selenium program.
            /// </summary>
            Selenium,
        }

        /// <summary>
        /// Gets the name of the testing driver.
        /// </summary>
        public TestingDriverType Name { get; }

        /// <summary>
        /// Gets the url of the page the webdriver is focued on.
        /// </summary>
        public string CurrentURL { get; }

        /// <summary>
        /// Gets or sets the loadiong spinner that appears on the website.
        /// </summary>
        public string LoadingSpinner { get; set; }

        /// <summary>
        /// Gets or sets the error container to check if any errors are shown on the UI.
        /// </summary>
        public string ErrorContainer { get; set; }

        /// <summary>
        /// Checks for an element state.
        /// </summary>
        /// <param name="xPath"> The xpath to find the web element. </param>
        /// <param name="state"> The state of the web element to wait for. </param>
        /// <returns> If the element state is as wanted.</returns>
        public bool CheckForElementState(string xPath, ElementState state);

        /// <summary>
        /// Performs the actions of clicking the specified element. Uses Selenium binding by default.
        /// </summary>
        /// <param name="xPath">The xpath to find the specified element.</param>
        /// <param name="byJS"> Whether to use JS to perform the click / not. </param>
        public void ClickElement(string xPath, bool byJS = false);

        /// <summary>
        /// Closes the current window. It will quit the browser if it is the last window opened.
        /// </summary>
        public void CloseBrowser();

        /// <summary>
        /// Accepts the alert provided that there is an alert.
        /// </summary>
        public void AcceptAlert();

        /// <summary>
        /// Dismisses the alert provided taht there is an alert.
        /// </summary>
        public void DismissAlert();

        /// <summary>
        /// Gets the text inside the alert.
        /// </summary>
        /// <returns>Alert Text.</returns>
        public string GetAlertText();

        /// <summary>
        /// Executes JS command on this element.
        /// </summary>
        /// <param name="jsCommand">command.</param>
        /// <param name="webElement">Elemnt to interact with.</param>
        public void ExecuteJS(string jsCommand, IWebElement webElement);

        /// <summary>
        /// Executes JS command on this element.
        /// </summary>
        /// <param name="jsCommand">command.</param>
        public void ExecuteJS(string jsCommand);

        /// <summary>
        /// Quits the webdriver. Call this when you want the driver to be closed.
        /// </summary>
        public void Quit();

        /// <summary>
        /// Maximizes the browser.
        /// </summary>
        public void Maximize();

        /// <summary>
        /// Force kill web driver.
        /// </summary>
        public void ForceKillWebDriver();

        /// <summary>
        /// Generates the AODA results.
        /// </summary>
        /// <param name="folderLocation"> The folder to generate AODA results in. </param>
        public void GenerateAODAResults(string folderLocation);

        /// <summary>
        /// The GetAllLinksURL.
        /// </summary>
        /// <returns>The <see cref="T:List{string}"/>.</returns>
        public List<string> GetAllLinksURL();

        /// <summary>
        /// Moves the mouse to the given element.
        /// </summary>
        /// <param name="element">Web element to mouse over.</param>
        public void MouseOver(IWebElement element);

        /// <summary>
        /// Tells the browser to navigate to the provided url.
        /// </summary>
        /// <param name="url">URL for the browser to navigate to.</param>
        /// <param name="instantiateNewDriver">Instantiates a new selenium driver.</param>
        /// <returns> <code>true</code> if the navigation was successful. </returns>
        public bool NavigateToURL(string url = "", bool instantiateNewDriver = true);

        /// <summary>
        /// Performs the action of populating a value.
        /// </summary>
        /// <param name="xPath"> The xpath to use to identify the element. </param>
        /// <param name="value"> The value to populate.</param>
        public void PopulateElement(string xPath, string value);

        /// <summary>
        /// Refreshes the webpage.
        /// </summary>
        public void RefreshWebPage();

        /// <summary>
        /// Method to run aoda on the current web page.
        /// </summary>
        /// <param name="providedPageTitle"> Title of the web page the user provides. </param>
        public void RunAODA(string providedPageTitle);

        /// <summary>
        /// The SendKeys.
        /// </summary>
        /// <param name="keystroke">The keystroke<see cref="string"/>.</param>
        public void SendKeys(string keystroke);

        /// <summary>
        /// Performs the action of selecting a value in an element.
        /// </summary>
        /// <param name="xPath"> The xpath to use to identify the element. </param>
        /// <param name="value"> The value to select in the element.</param>
        public void SelectValueInElement(string xPath, string value);

        /// <summary>
        /// Sets the global timeout in seconds.
        /// </summary>
        /// <param name="seconds">maximum duration of timeout.</param>
        public void SetTimeOutThreshold(string seconds);

        /// <summary>
        /// Switches to appropriate IFrame.
        /// </summary>
        /// <param name="xPath"> xPath to find the iFrame.</param>
        public void SwitchToIFrame(string xPath);

        /// <summary>
        /// The SwitchToTab.
        /// </summary>
        /// <param name="tab">The tab<see cref="int"/>.</param>
        public void SwitchToTab(int tab);

        /// <summary>
        /// Takes a screenshot of the browser. Screenshot will have the datestamp as its name. Year Month Date Hour Minutes Seconds (AM/PM).
        /// </summary>
        public void TakeScreenShot();

        /// <summary>
        /// Waits for an element state.
        /// </summary>
        /// <param name="xPath"> The xpath to find the web element. </param>
        /// <param name="state"> The state of the web element to wait for. </param>
        public void WaitForElementState(string xPath, ElementState state);

        /// <summary>
        /// Sets implicit wait timeout in seconds.
        /// </summary>
        /// <param name="seconds">Maximum timeout duration in seconds.</param>
        public void Wait(int seconds);

        /// <summary>
        /// Waits until the loading spinner disappears.
        /// </summary>
        public void WaitForLoadingSpinner();

        /// <summary>
        /// Checks if there are any errors in the error container.
        /// </summary>
        public void CheckErrorContainer();
    }
}
