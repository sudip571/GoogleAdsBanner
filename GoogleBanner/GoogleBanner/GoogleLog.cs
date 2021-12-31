using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleBanner
{
   public static  class GoogleLog
    {
        private static readonly object Locker = new object();
        public static void LogGoogleAds(string bannerName, string href)
        {
            try
            {
               
                lock (Locker)
                {
                    var fullPath = GoogleAds.Log_File_Path + "Logs.txt";
                    var fileInfo = new FileInfo(fullPath);
                    if (!fileInfo.Directory.Exists)
                        fileInfo.Directory.Create();
                    if (!File.Exists(fullPath))
                    {
                        File.Create(fullPath).Close();
                    }

                    

                    using (var writer = new StreamWriter(fullPath, true))
                    {
                        var builder = new StringBuilder();
                        builder.AppendLine("------------------------------------");
                        builder.AppendLine(string.Format("Banner Name: {0}", bannerName));
                        builder.AppendLine(string.Format("Link: {0}", href));
                        builder.AppendLine(string.Format("Timestamp: {0}", DateTime.Now));
                        builder.AppendLine("------------------------------------");
                        builder.AppendLine();
                        writer.WriteLine(builder.ToString());
                        writer.Close();
                    }
                }
              
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured while writing on txt file");
            }

        }
    }
}
