using System.Web;

namespace Sample
{
    // 參考: http://devco.re/blog/2014/06/19/client-ip-detection/

    public static class IpAddressHelper
    {
        public static RequestIpInfo GetRequestInfo()
        {
            var request = HttpContext.Current.Request;

            var info = new RequestIpInfo()
            {
                HTTP_CLIENT_IP = request.ServerVariables["HTTP_CLIENT_IP"],
                HTTP_X_FORWARDED_FOR = request.ServerVariables["HTTP_X_FORWARDED_FOR"],
                HTTP_X_FORWARDED = request.ServerVariables["HTTP_X_FORWARDED"],
                HTTP_X_CLUSTER_CLIENT_IP = request.ServerVariables["HTTP_X_CLUSTER_CLIENT_IP"],
                HTTP_FORWARDED_FOR = request.ServerVariables["HTTP_FORWARDED_FOR"],
                HTTP_FORWARDED = request.ServerVariables["HTTP_FORWARDED"],
                REMOTE_ADDR = request.ServerVariables["REMOTE_ADDR"],
                HTTP_VIA = request.ServerVariables["HTTP_VIA"]
            };

            return info;
        }

        public static string GetClientIP()
        {
            // 使用代理伺服器 Proxy 的情況
            // =======================================================
            // 直接連線 （沒有使用 Proxy）
            //
            // REMOTE_ADDR: 客戶端真實 IP
            // HTTP_VIA: 無
            // HTTP_X_FORWARDED_FOR: 無
            // =======================================================
            // Transparent Proxy
            //
            // REMOTE_ADDR: 最後一個代理伺服器 IP
            // HTTP_VIA: 代理伺服器 IP
            // HTTP_X_FORWARDED_FOR: 客戶端真實 IP，後以逗點串接多個經過的代理伺服器 IP
            // =======================================================
            // Anonymous Proxy
            //
            // REMOTE_ADDR: 最後一個代理伺服器 IP
            // HTTP_VIA: 代理伺服器 IP
            // HTTP_X_FORWARDED_FOR: 代理伺服器 IP，後以逗點串接多個經過的代理伺服器 IP
            // =======================================================
            // High Anonymity Proxy(Elite Proxy)
            //
            // REMOTE_ADDR: 代理伺服器 IP
            // HTTP_VIA: 無
            // HTTP_X_FORWARDED_FOR: 無(或以逗點串接多個經過的代理伺服器 IP)

            var ipInfo = GetRequestInfo();
            
            string clientIP = ipInfo.REMOTE_ADDR; //REMOTE_ADDR = HttpContext.Current.Request.UserHostAddress
            
            if (string.IsNullOrEmpty(ipInfo.HTTP_VIA) == false
                && string.IsNullOrEmpty(ipInfo.HTTP_X_FORWARDED_FOR) == false)
            {
                clientIP = ipInfo.HTTP_X_FORWARDED_FOR.Split(',')[0];
            }

            return clientIP;
        }
    }

    public class RequestIpInfo
    {
        public string HTTP_CLIENT_IP { get; set; }

        /// <summary>
        /// 客戶端真實 IP 或 Proxy IP
        /// </summary>
        public string HTTP_X_FORWARDED_FOR { get; set; }

        public string HTTP_X_FORWARDED { get; set; }

        public string HTTP_X_CLUSTER_CLIENT_IP { get; set; }

        public string HTTP_FORWARDED_FOR { get; set; }

        public string HTTP_FORWARDED { get; set; }

        /// <summary>
        /// 客戶端真實 IP 或是 Proxy IP
        /// </summary>
        public string REMOTE_ADDR { get; set; }

        /// <summary>
        /// 參考經過的 Proxy IP
        /// </summary>
        public string HTTP_VIA { get; set; }
    }
}
