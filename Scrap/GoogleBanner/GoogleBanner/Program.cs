using System;
using System.Web;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PuppeteerExtraSharp;
using PuppeteerExtraSharp.Plugins.AnonymizeUa;
using PuppeteerExtraSharp.Plugins.ExtraStealth;
using PuppeteerSharp;
using System.IO;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Drawing.Imaging;
using System.Drawing;
using System.Threading;

namespace GoogleBanner
{
   public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Program.GetGoogleAdsBannerAndLink().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured. Error Message is : " + ex.Message);
            }
            Console.WriteLine("Press any key to exit");
             Console.ReadKey();
        }
        public static async Task GetGoogleAdsBannerAndLink()
        {
           
            var urlList = GoogleAds.GetUrls();
            if(urlList.Count > 0)
            {
                foreach (var url in urlList)
                {
                    
                    #region setting chrome setup

                    ChromeDriverService service = ChromeDriverService.CreateDefaultService();
                    service.EnableVerboseLogging = false;
                    service.SuppressInitialDiagnosticInformation = true;
                    service.HideCommandPromptWindow = true;

                    ChromeOptions options = new ChromeOptions();
                    // to lunch chrome in Headless mode
                   // options.AddArgument("headless");
                    using (IWebDriver driver = new ChromeDriver(service,options))
                    {
                        driver.Manage().Window.Maximize();
                        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
                        Console.WriteLine("Sending request to " + url);
                        driver.Navigate().GoToUrl(url);
                        var iframeXpath = GoogleAds.GetMainIframeXpath();
                        // waiting 15 second to let the page load
                        Thread.Sleep(15000);
                        try
                        {   
                            // wait to see if <iframe> containing Google Ads is present in the page
                            var iframeElement = wait.Until(ExpectedConditions.ElementExists(By.XPath(iframeXpath)));
                            var resultt = driver.FindElements(By.XPath(iframeXpath));
                            Console.WriteLine("Looking for Google Ads Banner");
                            int bannerCount = 0;
                            for (int j = 0; j < resultt.Count; j++)
                            {
                                try
                                {
                                    var iframeContent = driver.SwitchTo().Frame(resultt[j]);
                                    if (iframeContent != null)
                                    {

                                        //check if Iframe has Href that redirects to ads page, if not there is no ads
                                        var anchorTagWithHref = iframeContent.FindElements(By.XPath(GoogleAds.GetAdsHrefXpath()));
                                        if (anchorTagWithHref != null && anchorTagWithHref.Count > 0)
                                        {
                                            bannerCount++;
                                            Console.WriteLine("Found Google Ads Banner : " + bannerCount);
                                            var hrefValue = anchorTagWithHref[0].GetAttribute("href");
                                            // extract Ads url . currently Query Key is "adurl". 
                                            Uri myUri = new Uri(hrefValue);
                                            string adsUrl = HttpUtility.ParseQueryString(myUri.Query).Get("adurl");
                                            var fileName = await GetBannerName(iframeContent.Url, bannerCount);
                                            await Program.SavePageScreenShot(fileName, resultt[j], driver);
                                            GoogleLog.LogGoogleAds(fileName + ".png", adsUrl);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    driver.SwitchTo().DefaultContent();
                                    continue;
                                }
                               
                                driver.SwitchTo().DefaultContent();
                            }
                            if (bannerCount == 0)
                            {
                                Console.WriteLine("No Google Ads Banner Found ");
                            }
                            else
                            {
                                Console.WriteLine("Google Banner Image has been saved and Details can be found in Logs.txt file");
                            }

                        }
                        catch (Exception ex)
                        {
                            
                        }
                        driver.Quit();
                        
                    }
                    #endregion
                   
                 
                }

            }
            else
                Console.WriteLine("No urls found");
        }

        public static async Task<string> GetBannerName(string pageUrl, int bannerNo)
        {
            var url = new Uri(pageUrl);
            //var fileName = url.Host.Replace('.', '_') + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff");
            var fileName = url.Host.Replace('.', '_') + "_"+ DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "_banner_" + bannerNo;
            return fileName;
        }
        public static async Task SavePageScreenShot(string fileName, IWebElement element, IWebDriver driver)
        {
            try
            {
                driver.SwitchTo().DefaultContent();
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
                //waiting 3 second to let the scroll complete
                Thread.Sleep(3000);
                // setting Red border around Ads Banner so that it would be easy to see in the screenshot
                IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;
                jsExecutor.ExecuteScript("arguments[0].style.border='4px solid red'", element);
                //waiting 3 second 
                Thread.Sleep(3000);
                var fullPath = GoogleAds.Banner_Image_Path + fileName + ".png";
                var fileInfo = new FileInfo(fullPath);
                if (!fileInfo.Directory.Exists)
                    fileInfo.Directory.Create();
                //Take the screenshot
                Screenshot image = ((ITakesScreenshot)driver).GetScreenshot();
                //Save the screenshot
                image.SaveAsFile(fullPath, ScreenshotImageFormat.Png);

                jsExecutor.ExecuteScript("arguments[0].style.border=''", element);
                //waiting 3 second 
                Thread.Sleep(3000);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured while saving banner image");
            }

        }
        public static async Task SaveNoBannerPageScreenShot(Page page)
        {
            try
            {
                var url = new Uri(page.Url);
                var fileName = url.Host.Replace('.', '_') + "_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                var fullPath = GoogleAds.No_Banner_Page_Image_Path + fileName + ".png";
                var fileInfo = new FileInfo(fullPath);
                if (!fileInfo.Directory.Exists)
                    fileInfo.Directory.Create();
                await page.ScreenshotAsync(fullPath, new ScreenshotOptions { FullPage = true });

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured while saving No Banner Page image");
            }
           


        }
        //public static async Task SavePageScreenShot(Page page, ElementHandle ele)
        //{
        //    var url = new Uri(page.Url);
        //    var fileName = url.Host.Replace('.', '_') + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff");
        //    var fullPath = @"C:\\FlightdeckLog\\PageScreenShot\\" + fileName + ".png";
        //    var fileInfo = new FileInfo(fullPath);
        //    if (!fileInfo.Directory.Exists)
        //        fileInfo.Directory.Create();
        //    //await page.ScreenshotAsync(fullPath, new ScreenshotOptions { FullPage = true });
        //    await ele.ScreenshotAsync(fullPath);

        //}
    }
}