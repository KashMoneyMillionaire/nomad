using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nomad.Library
{
    public class PhotoDigest
    {
        private readonly GooglePhotoClient _photoClient;
        private readonly EmailService _emailSvc;

        public PhotoDigest(GooglePhotoClient photoClient, EmailService emailSvc)
        {
            _photoClient = photoClient;
            _emailSvc = emailSvc;
        }

        public async Task RefreshAndSendEmail(string albumId, string albumLink, DateTimeOffset after)
        {
            // get photos since
            var (photoIds, newestPhotoTime) = await _photoClient.GetRecentPhotoIds(albumId, after);
            if (newestPhotoTime == null || !photoIds.Any())
                return;

            // send email
            await _emailSvc.SendDigest(albumLink, photoIds);
        }
    }
}