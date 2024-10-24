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
public class ModelsController : ControllerBase
{
    public record BucketObject(string name, string urn);

    public record TranslateObject(string bucketKey, string objectKey, string rootFilename, /*bool isSvf2 = true,*/ bool xAdsForce = false);

    private readonly APS _aps;

    public ModelsController(APS aps)
    {
        _aps = aps;
    }

    [HttpGet("{urn}/status")]
    public async Task<TranslationStatus> GetModelStatus(string urn)
    {
        var status = await _aps.GetTranslationStatus(urn);
        return status;
    }

    /// <summary>
    /// Base64 enconde a string
    /// </summary>
    public static string Base64Encode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }

    [HttpPost("{urn}/jobs")]
    public async Task<BucketObject> TranslateModel([FromRoute] string urn, [FromBody] TranslateObject data)
    {
        byte[] urnBytes = Convert.FromBase64String(urn);
        string objectId = System.Text.Encoding.UTF8.GetString(urnBytes);

        var job = await _aps.TranslateModel(objectId, data.rootFilename, data.xAdsForce);
        return new BucketObject(data.objectKey, job.Urn);
    }

    public class UploadModelForm
    {
        [FromForm(Name = "bucket-key")]
        public string BucketKey { get; set; }

        [FromForm(Name = "model-file")]
        public IFormFile File { get; set; }
    }

    [HttpPost(), DisableRequestSizeLimit]
    public async Task<ObjectDetails> UploadAndTranslateModel([FromForm] UploadModelForm form)
    {
        ObjectDetails objectDetails = null;
        using (var memoryStream = new MemoryStream())
        {
            await form.File.CopyToAsync(memoryStream);
            objectDetails = await _aps.UploadModel(form.BucketKey, form.File.FileName, memoryStream);
        }

        if (objectDetails == null)
        {
            HttpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
            return null;
        }

        return objectDetails;
    }
}
