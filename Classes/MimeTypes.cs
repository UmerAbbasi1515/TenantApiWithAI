using System;
using System.Collections.Generic;
using System.Linq;

public static class MimeTypes
{
    private static readonly Dictionary<string, byte[]> _mimeTypes = new Dictionary<string, byte[]>
        {
            {"image/jpeg", new byte[] {255, 216, 255}},
            {"image/jpg", new byte[] {255, 216, 255}},
            {"image/pjpeg", new byte[] {255, 216, 255}},
            {"image/apng", new byte[] {137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82}},
            {"image/png", new byte[] {137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82}},
            {"image/bmp", new byte[] {66, 77}},
            {"image/gif", new byte[] {71, 73, 70, 56}},
            {"application/pdf", new byte[] {37, 80, 68, 70, 45, 49, 46 } },
            {"application/msword", new byte[] {208, 207, 17, 224, 161, 177, 26, 225} },
            {"image/tiff", new byte[] {73, 73, 42, 0 } }
        };
    private static bool validateFileContent(byte[] file)
    {
        var data = false;
        foreach (var item in _mimeTypes)
        {
            var mimeBytes = item.Value;

            var fileBytes = file.Take(item.Value.Length);

            if (mimeBytes.SequenceEqual(fileBytes))
            {
                data = true;
            }


        }
        return data;
    }
    public static bool IsValidMimeType(byte[] file, string contentType, string ext)
    {
        var data = false;
        if (!string.IsNullOrWhiteSpace(ext))
        {
            switch (ext.ToLower())
            {
                case ".jpg":
                    data = true;
                    break;
                case ".jpeg":
                    data = true;
                    break;
                case ".png":
                    data = true;
                    break;
                case ".tiff":
                    data = true;
                    break;
                case ".pdf":
                    data = true;
                    break;
                case ".docx":
                    data = true;
                    break;
                case ".doc":
                    data = true;
                    break;
                case ".apng":
                    data = true;
                    break;
                case ".gif":
                    data = true;
                    break;
                case ".bmp":
                    data = true;
                    break;
                case ".pjpeg":
                    data = true;
                    break;
                default:
                    data = false;
                    break;
            }
        }
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            switch (contentType.ToLower())
            {
                case "image/jpg":
                    data = true;
                    break;
                case "image/jpeg":
                    data = true;
                    break;
                case "image/png":
                    data = true;
                    break;
                case "image/tiff":
                    data = true;
                    break;
                case "application/pdf":
                    data = true;
                    break;
                case "application/msword":
                    data = true;
                    break;

                case "image/apng":
                    data = true;
                    break;
                case "image/gif":
                    data = true;
                    break;
                case "image/bmp":
                    data = true;
                    break;
                case "image/pjpeg":
                    data = true;
                    break;
                default:
                    data = false;
                    break;
            }
        }
        data = validateFileContent(file);
        return data;
    }
}
