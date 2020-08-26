using System;
using System.Collections.Generic;
using System.Text;

namespace RedisMemoryCache.Net
{
    public delegate void TimeoutCallback(TimedMemoryCache.Net.TimedMemoryCache source, string key, dynamic value, long timeout, bool stale, bool removed);
}
