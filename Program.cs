using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace NotSoloLearn
{
    class Program
    {
        static string loginUrl = "https://www.sololearn.com/User/Login";
        static string scrapeUrl = "https://www.sololearn.com/Discuss/177817/what-country-are-you-from-leave-a-comment";

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("SoloLearn username password");
                Environment.Exit(1);
            }

            string userName = args[0];
            string passWord = args[1];

            Dictionary<string, int> countries = GetCountries();

            var driver = new PhantomJSDriver();
            driver.Navigate().GoToUrl(loginUrl);
            var userID = driver.FindElementById("Email");
            userID.Clear();
            userID.SendKeys(userName);
            var passID = driver.FindElementById("Password");
            passID.Clear();
            passID.SendKeys(passWord);

            var form = driver.FindElementByTagName("form");
            form.Submit();

            driver.Navigate().GoToUrl(scrapeUrl);

            int delay = 3000; // 3 seconds
            var javascriptInBrowser = (IJavaScriptExecutor)driver;
            var detailsWrapper = driver.FindElementsByClassName("detailsWrapper");
            var lastDetailsWrapperCount = detailsWrapper.Count;

            while (true)
            {
                javascriptInBrowser.ExecuteScript("arguments[0].scrollIntoView(true);",
                    detailsWrapper[lastDetailsWrapperCount - 1]);
                Thread.Sleep(delay);
                detailsWrapper = driver.FindElementsByClassName("detailsWrapper");
                if (detailsWrapper.Count == lastDetailsWrapperCount)
                {
                    break;
                }
                else
                {
                    lastDetailsWrapperCount = detailsWrapper.Count;
                    delay += 500; // add 1/2 a second to the delay 
                }
            }
            string text = string.Empty;
            foreach (var detail in detailsWrapper)
            {
                text = detail.GetAttribute("innerText").ToUpperInvariant();
                foreach (KeyValuePair<string, int> kvp in countries)
                {
                    if (text.Contains(kvp.Key.ToUpperInvariant()))
                    {
                        countries[kvp.Key] += 1;
                        break; // don't look for multiple matches
                    }
                }
            }

            driver.Quit();
            driver.Dispose();

            var items = from pair in countries
                        orderby pair.Value descending
                        select pair;

            foreach (KeyValuePair<string, int> kvp in items)
            {
                Console.WriteLine("{0}\t{1}", kvp.Key, kvp.Value);
            }
        }

        static string ArrangeName(string name)
        {
            string answer = string.Empty;
            int commaCount = 0;
            foreach (char c in name)
            {
                if (c == ',') commaCount++;
            }
            var splitter = new string[] { ", " };
            if (commaCount == 1)
            { // as in Korea, South
                var split = name.Split(splitter, StringSplitOptions.None);
                answer = String.Join(" ", split[1], split[0]);
            }
            else
            {
                answer = name;
            }
            return answer;
        }

        static Dictionary<string, int> GetCountries()
        {
            Dictionary<string, int> answer = new Dictionary<string, int>();
            var client = new WebClient();
            byte[] arr = client.DownloadData("https://www.cia.gov/library/publications/the-world-factbook/rankorder/rawdata_2102.txt");
            var str = System.Text.Encoding.Default.GetString(arr);
            string[] lines = str.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > 0)
                {
                    answer.Add(ArrangeName(lines[i].Substring(7, 50).Trim()), 0);
                }
            }


            List<KeyValuePair<string, int>> list = answer.ToList();

            list.Sort((firstPair, nextPair) =>
            {
                return firstPair.Key.Length.CompareTo(nextPair.Key.Length) * -1;
            }
            );

            answer = list.ToDictionary(pair => pair.Key, pair => pair.Value);

            return answer;
        }
    }
}
