using System;

namespace Server.Assistant {
    class Sys {
        public static long GetTimeStamp() {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);   //最后一个参数的意思？
            return Convert.ToInt64(ts.TotalSeconds);
        }
    }
}
