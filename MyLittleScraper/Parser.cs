using System.Text.RegularExpressions;

namespace MyLittleScraper
{
    public class Parser
    {
        private Regex imageRegex = new Regex("<img\\s+(?:[^>]*?\\s+)?src=([\"'])(.*?)\\1");
        private Regex cssRegex = new Regex("<link\\s+(?:[^>]*?\\s+)?href=([\"'])(.*?)\\1");
        private Regex jsRegex = new Regex("<script\\s+(?:[^>]*?\\s+)?src=([\"'])(.*?)\\1");
        private Regex linkRegex = new Regex("<a\\s+(?:[^>]*?\\s+)?href=([\"'])(.*?)\\1");


        public IEnumerable<string> FindFiles(string htmlString, Uri uri, FileType fileType)
        {
            var regex = GetRegex(fileType);
            var matches = regex.Matches(htmlString);
            var files = matches.Select(m => m.Groups[2].Value);
            return files.Select(im => $"{uri.Scheme}://{uri.Host}/{im}");
        }


        public IEnumerable<string> FindImages(string htmlString, Uri uri)
        {
            var matches = imageRegex.Matches(htmlString);
            var images = matches.Select(m => m.Groups[2].Value);
            return images.Select(im => $"{uri.Scheme}://{uri.Host}/{im}");
        }


        public IEnumerable<string> FindLinks(string htmlString, Uri uri)
        {
            var matches = linkRegex.Matches(htmlString);
            var paths = matches.Select(m => m.Groups[2].Value);
            return paths.Select(p => ResolveLink(p, uri));
        }


        // TODO: Make a link resolver that isn't awful
        private string ResolveLink(string link, Uri currentUri)
        {
            var origLinkSegments = link.Split('/');
            var segCount = link.Split('/').Count(s => s == "..");
            var linkSegments = origLinkSegments.Where(s => s != ".." && !string.IsNullOrWhiteSpace(s)).ToArray();

            var docSeg = currentUri.LocalPath.EndsWith(".html");
            var currSegCount = docSeg ? currentUri.Segments.Length - 1 : currentUri.Segments.Length;
            var steps = currSegCount - segCount;

            string newPath;

            if (segCount > 0)
                newPath = $"{currentUri.Scheme}://{currentUri.Host}" + string.Concat(currentUri.Segments[0..steps]) + string.Join('/', linkSegments);
            else
                newPath = $"{currentUri.Scheme}://{currentUri.Host}/" + string.Join('/', linkSegments);

            return newPath;
        }


        private Regex GetRegex(FileType fileType) =>
            fileType switch
            {
                FileType.JavaScript => jsRegex,
                _ => cssRegex
            };

        public enum FileType
        {
            JavaScript,
            CSS
        }
    }
}
