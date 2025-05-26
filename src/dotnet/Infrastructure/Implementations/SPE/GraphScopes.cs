using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Implementations.SPE
{
    public static class GraphScope
    {
        public const string Default = "https://graph.microsoft.com/.default";
        public const string FileStorageContainerSelected = "FileStorageContainer.Selected";
        public const string FilesReadAll = "Files.Read.All";
        public const string FilesRead = "Files.Read";
        public const string FilesReadWrite = "Files.ReadWrite";
        public const string FilesReadWriteAll = "Files.ReadWrite.All";
        public const string Profile = "profile";

        //initial permissions should be equal or less than the ones configured for the app in Azure. 
        public static readonly string[] InitialPermissions = new string[] { };
    }
}
