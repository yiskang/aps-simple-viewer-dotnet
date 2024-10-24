using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Oss.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class BucketsController : ControllerBase
{
    public record BucketInfo(string bucketKey, string policyKey = "persistent", string region = "US");

    private readonly APS _aps;

    public BucketsController(APS aps)
    {
        _aps = aps;
    }

    [HttpGet()]
    public async Task<IEnumerable<TreeNode>> GetOSSAsync([FromQuery(Name = "id")] string bucketKey)
    {
        var clientId = _aps.ClientId;
        IList<TreeNode> nodes = new List<TreeNode>();

        if (bucketKey == "#") // root
        {
            var buckets = await _aps.GetBuckets(100);
            foreach (var bucket in buckets)
            {
                nodes.Add(new TreeNode(bucket.BucketKey, bucket.BucketKey, "bucket", true));
            }
        }
        else
        {
            var objects = await _aps.GetObjects(bucketKey);
            foreach (var objInfo in objects)
            {
                nodes.Add(new TreeNode(Base64Encode(objInfo.ObjectId), objInfo.ObjectKey, "object", false));
            }
        }
        return nodes;
    }

    [HttpPost()]
    public async Task<Bucket> CreateBucket([FromBody] BucketInfo bucket)
    {
        var clientId = _aps.ClientId;
        var bucketKey = string.Format("{0}-{1}", bucket.bucketKey.ToLower(), clientId.ToLower());
        return await _aps.EnsureBucketExists(bucketKey, bucket.policyKey);
    }

    /// <summary>
    /// Model data for jsTree used on GetOSSAsync
    /// </summary>
    public class TreeNode
    {
        public TreeNode(string id, string text, string type, bool children)
        {
            this.id = id;
            this.text = text;
            this.type = type;
            this.children = children;
        }

        public string id { get; set; }
        public string text { get; set; }
        public string type { get; set; }
        public bool children { get; set; }
    }

    /// <summary>
    /// Base64 enconde a string
    /// </summary>
    public static string Base64Encode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }
}
