using System;
using System.IO;
using System.Net;

namespace Common.File
{
    // Sample:
    //  Common.File.RequestDownload clsDownload = new Common.File.RequestDownload();
    //  if( clsDownload.SaveHttpUrl("http://www.XXX.com/test.txt", @"C:\Test.txt"))
    //  {
    //      string txt = File.ReadAllText(@"C:\Test.txt");
    //  }
    //  else
    //  {
    //      Console.WriteLine(clsDownload.HttpStatus);
    //      Console.WriteLine(clsDownload.ErrorMessage);
    //  }

    public class RequestDownload
    {
        public HttpStatusCode HttpStatus { get { return _httpStatusCode; } }
        public FtpStatusCode FtpStatus { get { return _ftpStatusCode; } }
        public string StatusDescription { get { return _statusDescription; } }
        public string ErrorMessage { get { return _errorMessage; } }

        private IOnProgressChangedListener _onProgressChangedListener;
        private HttpStatusCode _httpStatusCode;
        private FtpStatusCode _ftpStatusCode;
        private string _statusDescription;
        private string _errorMessage;
        private Stream _responseStream; //non-seekable, forward-only, read-only stream
        private long _contentLength;
        private int _requestTimeout = 180; //second

        public bool SaveHttpUrl(string url, string savePath)
        {
            if(HttpDownload(url)) { return SaveFile(savePath); }
            return false;
        }

        public bool SaveFtpUrl(string url, string savePath, string userName, string password)
        {
            if (FtpDownload(url, userName, password)) { return SaveFile(savePath); }
            return false;
        }

        public string ReadHttpUrl(string url)
        {
            if (HttpDownload(url)) { return ReadStream(); }
            return string.Empty;
        }

        public string ReadFtpUrl(string url, string userName, string password)
        {
            if (FtpDownload(url, userName, password)) { return ReadStream(); }
            return string.Empty;
        }        

        private bool HttpDownload(string url)
        {
            bool complete = true;
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
            httpRequest.Timeout = _requestTimeout;
            HttpWebResponse httpResponse = null;
            
            try
            {
                httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                _httpStatusCode = httpResponse.StatusCode;
                _statusDescription = httpResponse.StatusDescription;

                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    _responseStream = httpResponse.GetResponseStream();
                    _contentLength = httpResponse.ContentLength;
                }
                else
                {
                    complete = false;
                }
            }
            catch (Exception ex)
            {
                complete = false;
                _errorMessage = ex.Message;
            }

            if (httpResponse != null) { httpResponse.Close(); }

            return complete;
        }

        private bool FtpDownload(string url, string userName, string password)
        {
            bool complete = true;
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(url);
            ftpRequest.Timeout = _requestTimeout;
            ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;
            ftpRequest.Credentials = new NetworkCredential(userName, password);
            FtpWebResponse ftpResponse = null;

            try
            {
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                _ftpStatusCode = ftpResponse.StatusCode;
                _statusDescription = ftpResponse.StatusDescription;

                if (ftpResponse.StatusCode == FtpStatusCode.FileActionOK)
                {
                    _responseStream = ftpResponse.GetResponseStream();
                    _contentLength = ftpResponse.ContentLength;
                }
                else
                {
                    complete = false;
                }
            }
            catch (Exception ex)
            {
                complete = false;
                _errorMessage = ex.Message;
            }

            if (ftpResponse != null) { ftpResponse.Close(); }

            return complete;
        }

        private bool SaveFile(string savePath)
        {
            bool complete = false;
            FileStream fs = null;

            if(_responseStream != null)
            {
                try
                {
                    fs = new FileStream(savePath, FileMode.Create, FileAccess.Write);
                    byte[] buffer = new byte[10 * 1024]; //10k
                    long downloadedSize = 0;
                    int size = 0;
                    do
                    {
                        size = _responseStream.Read(buffer, 0, buffer.Length);
                        if (size > 0)
                        {
                            fs.Write(buffer, 0, size);

                            // progress...
                            downloadedSize += size;
                            if (_onProgressChangedListener != null) { _onProgressChangedListener.OnProgressChangedListener(_contentLength, downloadedSize, false); }
                        }
                    } while (size > 0);

                    // completed
                    if (_onProgressChangedListener != null) { _onProgressChangedListener.OnProgressChangedListener(_contentLength, downloadedSize, true); }

                    complete = true;
                }
                catch (Exception ex)
                {                    
                    _errorMessage = ex.Message;
                }

                if (fs != null) { fs.Close(); }
                _responseStream.Close();
                _responseStream = null;
            }            

            return complete;
        }

        private string ReadStream()
        {
            string content = string.Empty;

            if(_responseStream != null)
            {
                using (StreamReader reader = new StreamReader(_responseStream))
                {
                    content = reader.ReadToEnd();
                }

                _responseStream.Close();
                _responseStream = null;
            }

            return content;
        }

        #region interface
        public void SetOnProgressChangedListener(IOnProgressChangedListener listener)
        {
            _onProgressChangedListener = listener;
        }

        public interface IOnProgressChangedListener
        {
            void OnProgressChangedListener(long contentSize, long downloadedSize, bool isCompleted);
        }
        #endregion interface        
    }
}
