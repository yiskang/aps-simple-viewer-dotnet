using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Autodesk.Oss;
using Autodesk.Oss.Model;

public partial class APS
{
    public async Task<Bucket> EnsureBucketExists(string bucketKey, string policyKey = "persistent", string region = "US")
    {
        var auth = await GetInternalToken();
        var ossClient = new OssClient(_sdkManager);
        try
        {
            return await ossClient.GetBucketDetailsAsync(auth.AccessToken, bucketKey);
        }
        catch (OssApiException ex)
        {
            if (ex.HttpResponseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var payload = new CreateBucketsPayload
                {
                    BucketKey = bucketKey,
                    PolicyKey = (PolicyKey)Enum.Parse(typeof(PolicyKey), policyKey, true)
                };
                return await ossClient.CreateBucketAsync(auth.AccessToken, (Region)Enum.Parse(typeof(Region), region, true), payload);
            }
            else
            {
                throw;
            }
        }
    }

    public async Task<ObjectDetails> UploadModel(string bucketKey, string objectKey, Stream fileToUpload)
    {
        await EnsureBucketExists(bucketKey);
        var auth = await GetInternalToken();
        var ossClient = new OssClient(_sdkManager);
        var objectDetails = await ossClient.Upload(bucketKey, objectKey, fileToUpload, auth.AccessToken, new System.Threading.CancellationToken());
        return objectDetails;
    }

    public async Task<IEnumerable<BucketsItems>> GetBuckets(int pageSize = 64)
    {
        var auth = await GetInternalToken();
        var ossClient = new OssClient(_sdkManager);
        var results = new List<BucketsItems>();
        var response = await ossClient.GetBucketsAsync(limit: pageSize, accessToken: auth.AccessToken);
        results.AddRange(response.Items);
        while (!string.IsNullOrEmpty(response.Next))
        {
            var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(response.Next).Query);
            response = await ossClient.GetBucketsAsync(limit: pageSize, startAt: queryParams["startAt"], accessToken: auth.AccessToken);
            results.AddRange(response.Items);
        }
        return results;
    }

    public async Task<IEnumerable<ObjectDetails>> GetObjects(string bucketKey, int pageSize = 64)
    {
        await EnsureBucketExists(bucketKey);
        var auth = await GetInternalToken();
        var ossClient = new OssClient(_sdkManager);
        var results = new List<ObjectDetails>();
        var response = await ossClient.GetObjectsAsync(auth.AccessToken, bucketKey, pageSize);
        results.AddRange(response.Items);
        while (!string.IsNullOrEmpty(response.Next))
        {
            var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(response.Next).Query);
            response = await ossClient.GetObjectsAsync(auth.AccessToken, bucketKey, pageSize, startAt: queryParams["startAt"]);
            results.AddRange(response.Items);
        }
        return results;
    }
}
