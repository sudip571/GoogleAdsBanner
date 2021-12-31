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
            int navigationTimeOut = 30000;  //time in milisecond
            int pageLoadingTime = 20000;    //time in milisecond
            var elementHandle = new List<ElementHandle>();
            var urlList = GoogleAds.GetUrls();
            if(urlList.Count > 0)
            {
                foreach (var url in urlList)
                {
                    int attemptCount = 1;
                    #region browser setup   
                    string[] argument = { "--start-maximized", "--no-zygote", "--no-sandbox", "--disable-setuid-sandbox", "--disable-features=site-per-process" };
                    string[] ignoredDefaultArgs = { "--disable-extensions" };
                    var chromiumPath = @"C:\\.local-chromium";
                    var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions
                    {
                        Path = chromiumPath
                    });
                    PuppeteerExtra puppeteerExtra = new PuppeteerExtra();
                    puppeteerExtra.Use(new AnonymizeUaPlugin()).Use(new StealthPlugin());
                    await browserFetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
                    var browser = await puppeteerExtra.LaunchAsync(new LaunchOptions
                    {
                        Headless = true,
                        IgnoreHTTPSErrors = true,
                        DefaultViewport = null,
                        Args = argument,
                        IgnoredDefaultArgs = ignoredDefaultArgs,
                        ExecutablePath = browserFetcher.RevisionInfo(BrowserFetcher.DefaultChromiumRevision).ExecutablePath
                        //ExecutablePath = "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe"
                    });
                    #endregion

                    var page = await browser.NewPageAsync();
                    await page.SetJavaScriptEnabledAsync(true);
                    await page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
                    {
                        ["user-agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.0 Safari/537.36",
                        ["accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3",
                        ["accept-encoding"] = "gzip, deflate, br",
                        ["accept-language"] = "en-US,en;q=0.9,en;q=0.8"
                    });
                    page.Response += (sender, e) =>
                    {
                        var headers = e.Response.Headers;
                        headers["Access-Control-Allow-Origin"] = "*";
                        headers["Access-Control-Allow-Credentials"] = "true";
                        headers["Access-Control-Allow-Methods"] = "GET,HEAD,OPTIONS,POST,PUT";
                        headers["Access-Control-Allow-Headers"] = "Access-Control-Allow-Headers, Origin,Accept, X-Requested-With, Content-Type, Access-Control-Request-Method, Access-Control-Request-Headers";
                    };

                    Console.WriteLine("\n**************************");
                    Console.WriteLine("Sending request to " + url);
                retry:
                    try
                    {
                        if (attemptCount > 1)
                            Console.WriteLine("Sending request one  more time to " + url);
                        await page.GoToAsync(url, timeout: navigationTimeOut, waitUntil: new WaitUntilNavigation[] { WaitUntilNavigation.DOMContentLoaded });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Could not load the page\n Message : " + ex.Message);
                        attemptCount++;
                        if (attemptCount > 2)
                            continue;
                        else
                            goto retry;
                    }
                    Console.WriteLine(string.Format("waiting for page to load properly (around {0} second)", pageLoadingTime/1000));
                    await page.WaitForTimeoutAsync(pageLoadingTime);

                    var iframeXpath = GoogleAds.GetMainIframeXpath();
                    var resultt = await page.XPathAsync(iframeXpath);
                    Console.WriteLine("Looking for Google Ads Banner");
                    int bannerCount = 0;
                    for (int j = 0; j < resultt.Length; j++)
                    {
                        var isVisible = await resultt[j].BoundingBoxAsync();
                        if (isVisible != null)
                        {
                            try
                            {
                                //get Iframe Content from page                       
                                var iFrame = await resultt[j].ContentFrameAsync();
                                //check if Iframe has Href that redirects to ads page, if not there is no ads
                                var anchorTagWithHref = await iFrame.XPathAsync(GoogleAds.GetAdsHrefXpath());
                                if (anchorTagWithHref != null && anchorTagWithHref.Length > 0)
                                {
                                    bannerCount++;
                                    Console.WriteLine("Found Google Ads Banner : " + bannerCount);
                                    var hrefValue = await anchorTagWithHref[0].EvaluateFunctionAsync<string>("e => e.href", anchorTagWithHref[0]);
                                    // extract Ads url . currently Query Key is "adurl". 
                                    Uri myUri = new Uri(hrefValue);
                                    string adsUrl = HttpUtility.ParseQueryString(myUri.Query).Get("adurl");
                                    var fileName = await GetBannerName(page, bannerCount);
                                    await Program.SavePageScreenShot(fileName, resultt[j]);
                                    GoogleLog.LogGoogleAds(fileName + ".png", adsUrl);
                                }

                            }
                            catch (Exception ex)
                            {
                                //Console.WriteLine("There is no content inside <iframe>.It is just an empty <iframe>");
                                continue;
                            }

                        }
                    }
                    if (bannerCount == 0)
                    {
                        Console.WriteLine("No Google Ads Banner Found (This may happen because the Requested site displays blank page instead)");
                        Console.WriteLine("See the page's image inside NoBannerPageImage folder");
                        await SaveNoBannerPageScreenShot(page);
                    }
                    else
                    {
                        Console.WriteLine("Google Banner Image has been saved and Details can be found in Logs.txt file");
                    }
                    await browser.CloseAsync();
                }

            }
            else
                Console.WriteLine("No urls found");
        }

        public static async Task<string> GetBannerName(Page page, int bannerNo)
        {
            var url = new Uri(page.Url);
            //var fileName = url.Host.Replace('.', '_') + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff");
            var fileName = url.Host.Replace('.', '_') + "_"+ DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "_banner_" + bannerNo;
            return fileName;
        }
        public static async Task SavePageScreenShot(string fileName, ElementHandle element)
        {
            try
            {  
                var fullPath = GoogleAds.Banner_Image_Path + fileName + ".png";
                var fileInfo = new FileInfo(fullPath);
                if (!fileInfo.Directory.Exists)
                    fileInfo.Directory.Create();
                await element.ScreenshotAsync(fullPath);

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