using StackExchange.Redis;
using System;

namespace Sample
{
    public class SerialNumberHelper
    {
        // 處理 thread-safe(安全執行緒) + lazy singleton(延遲載入實體) (for 4.0↑)
        private static Lazy<SerialNumberHelper> _instance = new Lazy<SerialNumberHelper>();
        private static readonly object _lockObj = new object();

        private SerialNumberHelper()
        {
        }

        public static SerialNumberHelper Current => _instance.Value;

        #region Redis 產生法

        private static ConnectionMultiplexer _redisConnection;

        private ConnectionMultiplexer RedisConnection
        {
            get
            {
                if (_redisConnection == null)
                {
                    lock (_lockObj)
                    {
                        if (_redisConnection == null)
                        {
                            _redisConnection = ConnectionMultiplexer.Connect("127.0.0.1:6379");
                        }

                        if (_redisConnection == null)
                        {
                            throw new AggregateException("Redis Server 连线错误");
                        }
                    }
                }

                return _redisConnection;
            }
        }

        private IDatabase RedisDB
        {
            get
            {
                int dbIndex = 0; // 0 - 15

                if (dbIndex >= 0)
                {
                    return this.RedisConnection.GetDatabase(dbIndex);
                }

                return this.RedisConnection.GetDatabase();
            }
        }

        /// <summary>
        /// 產生訂單編號
        /// </summary>
        public string GenerateByRedis()
        {
            lock (_lockObj)
            {
                // 基準時間
                DateTime epoch = DateTime.Now.Date;
                // 取得時間差
                long offset = (DateTime.Now.Ticks - epoch.Ticks);
                // 計算時間，利用時間差來當做 Key
                long ticks = offset / TimeSpan.FromSeconds(1).Ticks;
                string cacheKey = ticks.ToString();

                RedisDB.KeyExpire(cacheKey, TimeSpan.FromSeconds(10));
                long serialNo = RedisDB.StringIncrement(cacheKey);

                return $"{epoch.ToString("yyMMdd")}{ticks.ToString("00000")}{serialNo.ToString("0000")}";
            }
        }

        #endregion Redis 產生法

        #region 時間位元運算產生法 (Snowflake)
        
        // 設計概念參考 Twitter Snowflake
        // Twitter https://github.com/twitter/snowflake/releases/tag/snowflake-2010
        // 說明 http://www.lanindex.com/twitter-snowflake%EF%BC%8C64%E4%BD%8D%E8%87%AA%E5%A2%9Eid%E7%AE%97%E6%B3%95%E8%AF%A6%E8%A7%A3/
        // C# IdGen https://github.com/RobThree/IdGen

        private static int _sequence = 0;
        private static long _lastKey = -1;

        /// <summary>
        /// 產生訂單編號，以秒為單位，每秒最多產生 2047 筆
        /// </summary>
        /// <remarks>如要調整，需注意 ticks 的最大值</remarks>
        public string Generate()
        {
            lock (_lockObj)
            {
                // 基準時間
                DateTime epoch = DateTime.Now.Date;
                // 流水號最大值的位元數(12 bit = 4096)
                int sequenceBits = 12;
                // 流水號產生的最大數(4095)
                long sequenceMask = (1L << sequenceBits) - 1;

                // 取得時間差
                long offset = (DateTime.Now.Ticks - epoch.Ticks);
                // 計算時間，利用時間差當做 Key(每日最大秒數 = 86400)
                long ticks = offset / TimeSpan.FromSeconds(1).Ticks;

                if (ticks == _lastKey)
                {
                    // 判斷該組 Key 的序號是否已用完，序號 +1 後不可進位
                    if (_sequence >= sequenceMask)
                    {
                        return string.Empty;
                    }

                    _sequence++;
                }
                else
                {
                    _lastKey = ticks;
                    _sequence = 0;
                }

                // 先空出序號的位置，再加上序號
                long serialNo = (ticks << sequenceBits) + _sequence;

                return $"{DateTime.Now.ToString("yyMMdd")}{serialNo.ToString("000000000")}";
            }
        }

        #endregion 時間位元運算產生法 (Snowflake)
    }
}
