using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Proxy.Cache
{
    public class RedisCache : ICache
    {
        private readonly IDistributedCache redisCache;

        public RedisCache(IDistributedCache redisCache)
        {
            this.redisCache = redisCache;
        }
        public async Task<bool> ProcessCachedResponsePossibility(HttpContext context)
        {
            if (!HttpMethods.IsGet(context.Request.Method))
            {
                return false;
            }
            StringValues type;

            if (!context.Request.Headers.TryGetValue("Accept", out type))
            {
                return false;
            }
            var cachedRequest = await redisCache.GetAsync(context.Request.Path + type.First());

            if ((cachedRequest != null) && cachedRequest.Length != 0)
            {
                context.Response.StatusCode = (int)HttpStatusCode.AlreadyReported;
                context.Response.ContentType = type.First();
                if (type.First().Contains("json")) {
                    context.Response.Headers.Add("Content-Encoding", "gzip");
                }
                await context.Response.Body.WriteAsync(cachedRequest);

                return true;
            }

            return false;
        }

        public async Task WriteToCache(string key, byte[] content)
        {

            await redisCache.SetAsync(key, content, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            });
        }

        public async Task WriteToCache(string key, string content)
        {
            await redisCache.SetStringAsync(key, content, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            });
        }
    }
}
