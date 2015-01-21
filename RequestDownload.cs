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
        private string _statusDescription = string.Empty;
        private string _errorMessage = string.Empty;
        private HttpWebResponse _httpResponse = null;
        private FtpWebResponse _ftpResponse = null;
        private Stream _responseStream =  null; //non-seekable, forward-only, read-only stream
        private long _contentLength = 0;
        private int _requestTimeout = 180000; //ms

        public bool SaveHttpUrl(string url, string savePath)
        {
            if (HttpDownload(url)) { return SaveFile(savePath); }
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

        private void Close()
        {
            if (_httpResponse != null) { _httpResponse.Close(); _httpResponse = null; }
            if (_ftpResponse != null) { _ftpResponse.Close(); _ftpResponse = null; }
            if (_responseStream != null) { _responseStream.Close(); _responseStream = null; }            
        }

        private bool HttpDownload(string url)
        {
            bool complete = false;
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
            httpRequest.Timeout = _requestTimeout;
            try
            {
                _httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                _httpStatusCode = _httpResponse.StatusCode;
                _statusDescription = _httpResponse.StatusDescription;

                if (_httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    _responseStream = _httpResponse.GetResponseStream();
                    _contentLength = _httpResponse.ContentLength;

                    complete = true;
                }
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
                Close();
            }           

            return complete;
        }

        private bool FtpDownload(string url, string userName, string password)
        {
            bool complete = false;
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(url);
            ftpRequest.Timeout = _requestTimeout;            
            ftpRequest.Credentials = new NetworkCredential(userName, password);
            ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;          

            try
            {
                _ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                _ftpStatusCode = _ftpResponse.StatusCode;
                _statusDescription = _ftpResponse.StatusDescription;

                _responseStream = _ftpResponse.GetResponseStream();
                _contentLength = FtpDownload_GetFileSize(url, userName, password);

                complete = true;
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
                Close();
            }           

            return complete;
        }

        private long FtpDownload_GetFileSize(string url, string userName, string password)
        {
            long fileSize = 0;
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(url);
            ftpRequest.Timeout = _requestTimeout;
            ftpRequest.Credentials = new NetworkCredential(userName, password);
            ftpRequest.Method = WebRequestMethods.Ftp.GetFileSize;

            FtpWebResponse ftpResponse = null;
            try
            {
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                fileSize = ftpResponse.ContentLength;
            }
            catch (Exception) { }

            if (ftpResponse != null) { ftpResponse.Close(); }

            return fileSize;
        }

        private bool SaveFile(string savePath)
        {
            bool complete = false;
            FileStream fs = null;

            if (_responseStream != null)
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
                Close();                
            }

            return complete;
        }

        private string ReadStream()
        {
            string content = string.Empty;

            if (_responseStream != null)
            {
                using (StreamReader reader = new StreamReader(_responseStream))
                {
                    content = reader.ReadToEnd();
                }

                Close();
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
