using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Communication.Email;
using Azure.Communication.Email.Models;

namespace Nomad.Library;

public class EmailService
{
    public static string FileBase { get; set; }

    private static string Template => Path.Combine(FileBase, "template");
    private static readonly Lazy<string> _index = new(() => File.ReadAllText($"{Template}/index.html"));
    private static readonly Lazy<string> _day = new(() => File.ReadAllText($"{Template}/_day.html"));
    private static readonly Lazy<string> _photo = new(() => File.ReadAllText($"{Template}/_photo.html"));

    private readonly List<EmailAddress> _emailAddresses;
    private readonly EmailClient _emailClient;

    public EmailService(EmailClient emailClient, List<string> emailAddresses)
    {
        _emailAddresses = emailAddresses.Select(a => new EmailAddress(a)).ToList();
        _emailClient = emailClient;
    }

    public async Task SendDigest(string albumLink, List<(string Id, DateTimeOffset Date)> photoIds)
    {
        string emailBody = BuildBodyHtml(albumLink, photoIds);

        var content = new EmailContent("Weekly Nomad Update")
        {
            Html = emailBody
        };
        var recipients = new EmailRecipients(_emailAddresses);
        var emailMessage = new EmailMessage("digest@nomad.kash.money", content, recipients);

        await _emailClient.SendAsync(emailMessage);
    }

    private static string BuildBodyHtml(string albumLink, List<(string Id, DateTimeOffset Date)> photoIds)
    {
        return _index.Value.Replace("{{week}}", DateTimeOffset.UtcNow.ToString("MMM dd"))
                     .Replace("{{days}}", BuildDaysHtml(albumLink, photoIds));
    }

    private static string BuildDaysHtml(string albumLink, List<(string Id, DateTimeOffset Date)> photoIds)
    {
        var days = photoIds.GroupBy(p => p.Date.Date)
                           .Select(d => _day.Value.Replace("{{day}}", d.Key.ToString("M"))
                                            .Replace("{{photos}}", BuildPhotosHtml(albumLink, d)));

        return string.Join(" ", days);
    }

    private static string BuildPhotosHtml(string albumLink, IEnumerable<(string Id, DateTimeOffset Date)> day)
    {
        var photos = day.Select(d => _photo.Value.Replace("{{albumUrl}}", albumLink)
                                           .Replace("{{thumbnailUrl}}",
                                                    $"https://kash-nomad.azurewebsites.net/api/photos?photoId={d.Id}"));

        return string.Join(" ", photos);
    }
}