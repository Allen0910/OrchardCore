using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using OrchardCore.Modules.FileProviders;

namespace OrchardCore.Modules
{
    /// <summary>
    /// This custom <see cref="IFileProvider"/> implementation provides the file contents
    /// of embedded files in Module assemblies.
    /// </summary>
    public class ModuleEmbeddedFileProvider : IFileProvider
    {
        private const string Root = ".Modules";
        private const string RootWithTrailingSlash = ".Modules/";
        private IHostingEnvironment _environment;
        private string _contentPathWithTrailingSlash;

        public ModuleEmbeddedFileProvider(IHostingEnvironment hostingEnvironment, string contentPath = null)
        {
            _environment = hostingEnvironment;
            _contentPathWithTrailingSlash = contentPath != null ? NormalizePath(contentPath) + '/' : "";
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            if (subpath == null)
            {
                return NotFoundDirectoryContents.Singleton;
            }

            var folder = _contentPathWithTrailingSlash + NormalizePath(subpath);

            var entries = new List<IFileInfo>();

            if (folder == "")
            {
                entries.Add(new EmbeddedDirectoryInfo(Root));
            }
            else if (folder == Root)
            {
                entries.AddRange(_environment.GetApplication().ModuleNames
                    .Select(n => new EmbeddedDirectoryInfo(n)));
            }
            else if (folder.StartsWith(RootWithTrailingSlash, StringComparison.Ordinal))
            {
                var underRootPath = folder.Substring(RootWithTrailingSlash.Length);

                var index = underRootPath.IndexOf('/');
                var name = index == -1 ? underRootPath : underRootPath.Substring(0, index);

                var directories = new HashSet<string>();
                var paths = _environment.GetModule(name).AssetPaths;

                foreach (var path in paths.Where(p => p.StartsWith(folder, StringComparison.Ordinal)))
                {
                    var underDirectoryPath = path.Substring(folder.Length + 1);
                    index = underDirectoryPath.IndexOf('/');

                    if (index == -1)
                    {
                        entries.Add(GetFileInfo(path));
                    }
                    else
                    {
                        directories.Add(underDirectoryPath.Substring(0, index));
                    }
                }

                entries.AddRange(directories.Select(f => new EmbeddedDirectoryInfo(f)));
            }

            return new EmbeddedDirectoryContents(entries);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            if (subpath == null)
            {
                return new NotFoundFileInfo(subpath);
            }

            var path = _contentPathWithTrailingSlash + NormalizePath(subpath);

            if (path.StartsWith(RootWithTrailingSlash, StringComparison.Ordinal))
            {
                var underRootPath = path.Substring(RootWithTrailingSlash.Length);
                var index = underRootPath.IndexOf('/');

                if (index != -1)
                {
                    var name = underRootPath.Substring(0, index);
                    var module = _environment.GetModule(name);
                    return module.GetFileInfo(path);
                }
            }

            return new NotFoundFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            return NullChangeToken.Singleton;
        }

        private string NormalizePath(string path)
        {
            return path.Replace('\\', '/').Trim('/');
        }
    }
}
