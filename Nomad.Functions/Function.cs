using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Nomad.Library;

namespace Nomad.Functions
{
    public static class Function
    {
        private static AppConfig GetConfig()
        {
            return new AppConfig
            {
                ClientId = Environment.GetEnvironmentVariable("CLIENT_ID"),
                ClientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET"),
                RefreshToken = Environment.GetEnvironmentVariable("REFRESH_TOKEN"),
                AlbumId = Environment.GetEnvironmentVariable("ALBUM_ID"),
                AlbumLink = Environment.GetEnvironmentVariable("ALBUM_LINK"),
                EmailKey = Environment.GetEnvironmentVariable("EMAIL_KEY"),
                EmailDomain = Environment.GetEnvironmentVariable("EMAIL_DOMAIN"),
                EmailAddresses = Environment.GetEnvironmentVariable("EMAILS").Split(";").ToList()
            };
        }
        
        [FunctionName("WeeklyDigest")]
        public static async Task WeeklyDigest([TimerTrigger("0 14 * * MON")] TimerInfo myTimer, ILogger log,
                                              ExecutionContext context)
        {
            var cfg = GetConfig();
            
            var googlePhotoClient = new GooglePhotoClient(cfg.ClientId, cfg.ClientSecret, cfg.RefreshToken);
            var credentials = new AzureKeyCredential(cfg.EmailKey);
            EmailService.FileBase = context.FunctionAppDirectory;
            var emailClient = new EmailClient(new Uri(cfg.EmailDomain), credentials);
            var email = new EmailService(emailClient, cfg.EmailAddresses);
            var photoDigest = new PhotoDigest(googlePhotoClient, email);
            
            await photoDigest.RefreshAndSendEmail(cfg.AlbumId, cfg.AlbumLink, DateTimeOffset.UtcNow.AddDays(-7));
        }

        [FunctionName("RefreshBearer")]
        public static async Task RefreshBearer([TimerTrigger("*/55 * * * *")] TimerInfo myTimer, ILogger log)
        {
            var cfg = GetConfig();
            
            var googlePhotoClient = new GooglePhotoClient(cfg.ClientId, cfg.ClientSecret, cfg.RefreshToken);

            await googlePhotoClient.GetBearerToken(true);
        }

        [FunctionName("GetPhoto")]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "photos")] HttpRequest req, ILogger log)
        {
            var cfg = GetConfig();
            string photoId = req.Query["photoId"];
            
            var googlePhotoClient = new GooglePhotoClient(cfg.ClientId, cfg.ClientSecret, cfg.RefreshToken);
            var url = await googlePhotoClient.GetMediaUrl(photoId);

            return new RedirectResult(url, false);

        }
    }

    internal class AppConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RefreshToken { get; set; }
        public string AlbumId { get; set; }
        public string AlbumLink { get; set; }
        public string EmailKey { get; set; }
        public string EmailDomain { get; set; }
        public List<string> EmailAddresses { get; set; }
    }
}