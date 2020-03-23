// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace TestingDriver
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// .
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            ITestAutomationDriver driver = new SeleniumDriver();
            driver.NavigateToURL("https://www.google.ca");
            driver.Quit();
        }
    }
}
