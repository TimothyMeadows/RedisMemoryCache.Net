# RedisMemoryCache.Net
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) [![nuget](https://img.shields.io/nuget/v/RedisMemoryCache.Net.svg)](https://www.nuget.org/packages/RedisMemoryCache.Net/)

Implementation of a parallel thread-safe in-memory caching system with save, and load support suited for 'state' programming and easy timeout support for time sensitive caching with [Redis](https://redis.io) support. Depends on [MemoryCache.Net](https://github.com/TimothyMeadows/MemoryCache.Net) and Depends on [TimedMemoryCache.Net](https://github.com/TimothyMeadows/TimedMemoryCache.Net)

# Install

From a command prompt
```bash
dotnet add package RedisMemoryCache.Net
```

```bash
Install-Package RedisMemoryCache.Net
```

You can also search for package via your nuget ui / website:

https://www.nuget.org/packages/RedisMemoryCache.Net/

# Examples

You can find more examples in the github examples project.

```csharp
// You can populate the cache at initialization time, it will inherit the "default" timeout, and the default "expires" set in the constructor.
var cache = new RedisMemoryCache(connectionString, 3, TimeSpan.FromSeconds(30))
{
  ["caw"] = "caw caw caw", 
  ["caw2"] = new KeyValuePair<string, string>("caw", "caw caw caw")
};

// You can also access an entry directly by key. However you will need to cast from dynamic using this method.
var caw1 = (string)cache["caw"];

// You can set an entry directly. It will inherit the "default" timeout, and the default "expires" set in the constructor.
cache["caw3"] = "third caw!";

// You can still set non-serializable values in memory, however, they will NOT be stored in Redis, and will inherit the "default" timeout, and the default "expires" set in the constructor.
// You can override the timeout by setting it to 0 in which case it will remain in memory until .Dispose
cache.Write("caw4", new MemoryStream(new byte[] {32, 34}), 0);

// You can set the timeout, and expires directly using the Write method.
cache.Write("caw5", true, 10, TimeSpan.FromMinutes(2));


// When cache entries expire they will first be checked in redis, if they exist, they will be re-added with the same timeout they were set with.
// If they do not exist in redis they will be removed, finally if redis times out during a read, the previous value will be used but considered "stale"
cache.OnTimeout += (source, key, value, timeout, stale, removed) =>
{
  Console.WriteLine($"{key}: stale={stale} removed={removed}");
};
```

# Constructor

Set's the default timeout for all writes that do not specify there own timeout. Can also set the timespan for redis writes that do not specify there own. Finally synchronize can be set to false if you do not want your write, and deletes to be synchronized with Redis by default.

```csharp
RedisMemoryCache(string connectionString, int seconds = 300, TimeSpan? expires = null, bool synchronize = true)
```

# Methods

Write a dynamic value to cache with default timeout without returning anything
```csharp
void Write(string key, dynamic value)
```

Write a dynamic value to cache with own timeout without returning anything
```csharp
void Write(string key, dynamic value, long timeout)
```

Write a dynamic value to cache with own timeout without returning anything and setting the redis value to expire when the TimeSpan is reached.
```csharp
void Write(string key, dynamic value, long timeout, TimeSpan? expires)
```

Write a dynamic value to cache with own timeout without returning anything and setting the redis value to expire when the TimeSpan is reached. You can also specify synchronize as false if you wish to write the value to memory only.
```csharp
void Write(string key, dynamic value, long timeout, TimeSpan? expires, bool synchronize)
```

Write a T value to cache with default timeout returning the T value from cache
```csharp
T Write<T>(string key, T value)
```

Write a T value to cache with own timeout returning the T value from cache
```csharp
T Write<T>(string key, T value, long timeout)
```

Write a T value to cache with own timeout returning the T value from cache and setting the redis value to expire when the TimeSpan is reached.
```csharp
T Write<T>(string key, T value, long timeout, TimeSpan? expires)
```

Write a T value to cache with own timeout returning the T value from cache and setting the redis value to expire when the TimeSpan is reached. You can also specify synchronize as false if you wish to write the value to memory only.
```csharp
T Write<T>(string key, T value, long timeout, TimeSpan? expires, bool synchronize)
```

Read a value from cache returning as T
```csharp
T Read<T>(string key)
```

Delete an entry from cache without returning anything
```csharp
void Delete(string key)
```

Delete an entry from cache without returning anything. You can also specify synchronize as false if you wish to delete the value from memory only.
```csharp
void Delete(string key, bool synchronize)
```

Delete an entry from cache returning that value as T
```csharp
T Delete<T>(string key)
```

Delete an entry from cache returning that value as T. You can also specify synchronize as false if you wish to delete the value from memory only.
```csharp
T Delete<T>(string key, bool synchronize)
```
