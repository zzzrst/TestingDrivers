using System;
using System.Collections.Generic;
using System.Text;

namespace TestingDriver
{
    class Class1
    {
        public static void Main(string[] args)
        {
            ITestingDriver driver = new SeleniumDriver();
            driver.NavigateToURL("https://www.google.ca");
            driver.Quit();
        }
    }
}
