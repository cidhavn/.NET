using System;
using System.Net;

namespace Common.File
{
    // 注意: WebClient不帶timeout屬性，如果URL不存在會「當」在那邊
    // Samlpe:
    //   Common.File.WebClientDownload clsDownload = new Common.File.WebClientDownload();
    //   if( clsDownload.SaveHttpUrl("http://www.XXX.com/test.txt", @"C:\Test.txt"))
    //   {
    //       string txt = File.ReadAllText(@"C:\Test.txt");
    //   }
    //   else
    //   {
    //       Console.WriteLine(clsDownload.ErrorMessage);
    //   }

    public class WebClientDownload
    {
        public string ErrorMessage { get { return _errorMessage; } }

        private string _errorMessage;

        public bool SaveHttpUrl(string url, string savePath)
        {
            WebClient wc = new WebClient();
            try
            {
                wc.DownloadFile(url, savePath);
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
                return false;
            }
            return true;
        }

        public bool SaveFtpUrl(string url, string savePath, string userName, string password)
        {
            WebClient wc = new WebClient();
            wc.Credentials = new NetworkCredential(userName, password);
            try
            {
                wc.DownloadFile(url, savePath);
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
                return false;
            }
            return true;
        }

        public string ReadHttpUrl(string url)
        {
            WebClient wc = new WebClient();
            try
            {
                return wc.DownloadString(url);
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
                return string.Empty;
            }
        }

        public string ReadFtpUrl(string url, string userName, string password)
        {
            WebClient wc = new WebClient();
            wc.Credentials = new NetworkCredential(userName, password);
            try
            {
                return wc.DownloadString(url);
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
                return string.Empty;
            }
        }
    }
}
