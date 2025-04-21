using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace GreekRecruit.Services;

public class S3Service
{
    private readonly IAmazonS3 _s3Client;
    private const string BucketName = "greek-recruit-uploads";

    public S3Service(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }

    public async Task UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var transferUtility = new TransferUtility(_s3Client);
        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = fileStream,
            Key = fileName,
            BucketName = BucketName,
            ContentType = contentType,
            CannedACL = S3CannedACL.Private // or S3CannedACL.PublicRead
        };
        await transferUtility.UploadAsync(uploadRequest);
    }

    public string GetFileUrl(string fileName)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = BucketName,
            Key = fileName,
            Expires = DateTime.UtcNow.AddHours(1),
        };
        return _s3Client.GetPreSignedURL(request);
    }

    public async Task<Stream> DownloadFileAsync(string fileName)
    {
        var response = await _s3Client.GetObjectAsync(BucketName, fileName);
        return response.ResponseStream;
    }
}
