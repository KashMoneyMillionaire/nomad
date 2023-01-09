using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Email;
using Nomad.Library;

namespace Nomad.Console
{
    class Program
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
                EmailAddresses = Environment.GetEnvironmentVariable("EMAILS")!.Split(";").ToList()
            };
        }

        static async Task Main(string[] args)
        {
            var cfg = GetConfig();
            
            var googlePhotoClient = new GooglePhotoClient(cfg.ClientId, cfg.ClientSecret, cfg.RefreshToken);
            var credentials = new AzureKeyCredential(cfg.EmailKey);
            var emailClient = new EmailClient(new Uri(cfg.EmailDomain), credentials);
            var email = new EmailService(emailClient, cfg.EmailAddresses);
            var photoDigest = new PhotoDigest(googlePhotoClient, email);
            
            await photoDigest.RefreshAndSendEmail(cfg.AlbumId, cfg.AlbumLink, DateTimeOffset.UtcNow.AddDays(-7));
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