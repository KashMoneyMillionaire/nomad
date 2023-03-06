using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;

namespace Nomad.Library
{
    public class GooglePhotoClient
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _refreshToken;
        private static (DateTimeOffset ExpiresOn, string BearerToken)? _cache;

        public GooglePhotoClient(string clientId, string clientSecret, string refreshToken)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _refreshToken = refreshToken;
        }

        public async Task<string> GetBearerToken(bool overrideCache = false)
        {
            try
            {
                if (!overrideCache && _cache != null && _cache.Value.ExpiresOn > DateTimeOffset.UtcNow)
                    return _cache.Value.BearerToken;

                dynamic json = await "https://oauth2.googleapis.com/token"
                                    .PostUrlEncodedAsync(new
                                     {
                                         client_id = _clientId,
                                         client_secret = _clientSecret,
                                         refresh_token = _refreshToken,
                                         grant_type = "refresh_token"
                                     })
                                    .ReceiveJson();

                var expires = (int)json.expires_in;
                string bearerToken = (string)json.access_token;
                _cache = (DateTimeOffset.UtcNow.AddSeconds(expires - 60), bearerToken);

                return bearerToken;
            }
            catch (FlurlHttpException e)
            {
                Console.WriteLine(e);
                Console.WriteLine(await e.Call.HttpResponseMessage.Content.ReadAsStringAsync());
                throw;
            }
        }

        public async Task<(List<(string, DateTimeOffset)>, DateTimeOffset?)> GetRecentPhotoIds(
            string albumId, DateTimeOffset after)
        {
            try
            {
                string? pageToken = null;
                List<(string Id, DateTimeOffset CreationTime)> photoIds = new();
                DateTimeOffset? newSync = null;

                do
                {
                    var response = await "https://photoslibrary.googleapis.com/v1/mediaItems:search"
                                        .WithOAuthBearerToken(await GetBearerToken())
                                        .PostJsonAsync(new
                                         {
                                             pageSize = 100,
                                             albumId,
                                             pageToken
                                         })
                                        .ReceiveJson<Response>();

                    pageToken = response.NextPageToken;

                    photoIds.AddRange(response.MediaItems.Where(i => i.MediaMetadata.CreationTime > after)
                                              .Select(item => (item.Id, item.MediaMetadata.CreationTime)));
                    var latestFound = response.MediaItems.Max(i => i.MediaMetadata.CreationTime);
                    newSync = newSync is null || latestFound > newSync ? latestFound : newSync;
                } while (pageToken != null);

                return (photoIds, newSync);
            }
            catch (FlurlHttpException e)
            {
                Console.WriteLine(e);
                Console.WriteLine(await e.Call.HttpResponseMessage.Content.ReadAsStringAsync());
                throw;
            }
        }

        public async Task<string> GetMediaUrl(string photoId)
        {
            var response = await $"https://photoslibrary.googleapis.com/v1/mediaItems/{photoId}"
                                .WithOAuthBearerToken(await GetBearerToken())
                                .GetJsonAsync();

            return response.baseUrl.ToString();
        }
    }

    class Response
    {
        public List<MediaItem> MediaItems { get; set; }
        public string? NextPageToken { get; set; }
    }

    class MediaItem
    {
        public string Id { get; set; }
        public MediaItemMetadata MediaMetadata { get; set; }
    }

    internal class MediaItemMetadata
    {
        public DateTimeOffset CreationTime { get; set; }
    }
}