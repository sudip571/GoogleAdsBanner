using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleBanner
{
   public static  class GoogleAds
    {
        public static string Log_File_Path = @"C:\GoogleBanner\";
        public static string Banner_Image_Path = @"C:\GoogleBanner\BannerImage\";
        public static string No_Banner_Page_Image_Path = @"C:\GoogleBanner\NoBannerPageImage\";
        public static List<string> GetUrls()
        {
            //add all urls here
            var urls = new List<string>()
            {
                "https://v-kosmose.com/goroskop/",
                //"https://www.biathlon.com.ua/ua/",
                //"https://cocoin.su/",
                //"https://infotime.co/",
                //"https://www.astro-seek.com/"

            };
            return urls;
        }

        public static string GetMainIframeXpath()
        {
            //Add here other Key and its value that distinguish the Iframe as Google ADS
            var attributeAndValue = new List<KeyValuePair<string, string>>();
            attributeAndValue.Add(new KeyValuePair<string, string>("src", "googleads")); 
            attributeAndValue.Add(new KeyValuePair<string, string>("src", "doubleclick"));
            attributeAndValue.Add(new KeyValuePair<string, string>("id", "google_ads"));


            // //iframe[contains(@src, ""googleads"") or contains(@src, ""doubleclick"")]"
            var xpathFormat = @"contains(@{0}, ""{1}"")";
            var xpath = "//iframe[";
            for (int i = 0; i < attributeAndValue.Count; i++)
            {
                if (i != 0)
                    xpath = xpath + " or ";
                var dict = attributeAndValue.ElementAt(i);
                xpath = xpath + string.Format(xpathFormat, dict.Key, dict.Value);
            }
            xpath = xpath + "]";
            var tt = GetAdsHrefXpath();
            return xpath;
            
        }
        public static string GetAdsHrefXpath()
        {
            //Add here other Key and its value that distinguish the Anchor tag with ads redirect href
            var attributeAndValue = new List<KeyValuePair<string, string>>();
            attributeAndValue.Add(new KeyValuePair<string, string>("href", "googleads"));
            attributeAndValue.Add(new KeyValuePair<string, string>("href", "doubleclick"));



            // //a[contains(@href, ""googleads"") or contains(@href, ""doubleclick"")]
            var xpathFormat = @"contains(@{0}, ""{1}"")";
            var xpath = "//a[";
            for (int i = 0; i < attributeAndValue.Count; i++)
            {
                if (i != 0)
                    xpath = xpath + " or ";
                var dict = attributeAndValue.ElementAt(i);
                xpath = xpath + string.Format(xpathFormat, dict.Key, dict.Value);
            }
            xpath = xpath + "]";
            return xpath;

        }

    }

   
}
