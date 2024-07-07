using System;
using System.IO;
using System.Security.Permissions;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NewsAggregation.Controllers
{
    [ApiController]
    [Route("files")]
    public class StorageController : ControllerBase
    {
        private readonly IAmazonS3 _s3Client;
        private const string BucketName = "personal-podcast-life-2";

        public StorageController(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        [HttpPost, Authorize(Roles = "Admin,SuperAdmin")]
        [Route("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            try
            {

                    if (file == null || file.Length == 0)
                        return BadRequest(new { Message = "No file provided.", Code = 55 });

                    if (!file.ContentType.Contains("image") && !file.ContentType.Contains("audio") && !file.ContentType.Contains("video"))
                    {
                        return BadRequest(new { Message = "File type not supported. Please upload an image, audio or video file.", Code = 57 });
                    }

                    var fileExtension = Path.GetExtension(file.FileName);
                    var s3ObjectKey = Guid.NewGuid().ToString();
                    var s3ObjectKeyWithExtension = $"{s3ObjectKey}{fileExtension}";

                    var multipartUploadRequest = new InitiateMultipartUploadRequest
                    {
                        BucketName = BucketName,
                        Key = s3ObjectKeyWithExtension
                    };

                    var initResponse = await _s3Client.InitiateMultipartUploadAsync(multipartUploadRequest);

                    // Split the file into parts and upload each part
                    var partSize = 5 * 1024 * 1024; // 5 MB
                    var fileTransferUtility = new TransferUtility(_s3Client);
                    var uploadResponses = new List<UploadPartResponse>();

                    using (var fileStream = file.OpenReadStream())
                    {
                        int partNumber = 1;
                        for (long i = 0; i < file.Length; i += partSize)
                        {
                            var size = Math.Min(partSize, file.Length - i);
                            var buffer = new byte[size];
                            await fileStream.ReadAsync(buffer, 0, (int)size);

                            var uploadRequest = new UploadPartRequest
                            {
                                BucketName = BucketName,
                                Key = s3ObjectKeyWithExtension,
                                UploadId = initResponse.UploadId,
                                PartNumber = partNumber++,
                                PartSize = size,
                                InputStream = new MemoryStream(buffer)
                            };

                            var uploadPartResponse = await _s3Client.UploadPartAsync(uploadRequest);
                            uploadResponses.Add(uploadPartResponse);
                        }
                    }

                    // Complete the multipart upload
                    var completeRequest = new CompleteMultipartUploadRequest
                    {
                        BucketName = BucketName,
                        Key = s3ObjectKeyWithExtension,
                        UploadId = initResponse.UploadId,
                        PartETags = uploadResponses.Select(x => new PartETag { PartNumber = x.PartNumber, ETag = x.ETag }).ToList()
                    };

                    await _s3Client.CompleteMultipartUploadAsync(completeRequest);

                    var s3ObjectUrl = $"https://{BucketName}.s3.amazonaws.com/{s3ObjectKeyWithExtension}";
                    return Ok(new { Code = 60, Message = "File uploaded successfully!", S3ObjectUrl = s3ObjectUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error uploading file: {ex.Message}");
            }
        }


        [HttpDelete, Authorize(Roles = "Admin,SuperAdmin")]
        [Route("delete")]
        public async Task<IActionResult> DeleteFile(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return BadRequest(new { Message = "No key provided.", Code = 58 });

                await _s3Client.DeleteObjectAsync(BucketName, key);

                return Ok(new { Code = 59, Message = "File deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting file: {ex.Message}");
            }
        }
    }
}
