using System;
using System.Collections.Generic;
using Renci.SshNet;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Azure.Services.AppAuthentication;

namespace SftpWatcher
{
    // Checks the list of files in an SFTP folder and emits events for newly added, removed and modified files.
    public abstract class SftpWatcherEntity : ISftpWatcherEntity
    {
        public IDictionary<string, DateTime> files { get; set; }
        public string error { get; set; }

        // Does the actual job
        public void Watch(string folderFullPath)
        {
            bool isFirstRun = this.files == null;
            if (isFirstRun)
            {
                this.files = new Dictionary<string, DateTime>();
            }

            this.error = string.Empty;
            try
            {
                // Intentionally NOT passing any creds via parameters or entity state. Don't want them to be stored anywhere.
                this.GetParams(folderFullPath, out var serverName, out var folderName, out var fileMask, out var userName, out var password);

                using (var client = new SftpClient(serverName, userName, password))
                {
                    client.Connect();

                    var maskRegex = new Regex(Regex.Escape(fileMask).Replace("\\*", ".+"));
                    var newFiles = ListFilesInFolder(client, folderName, maskRegex);

                    // Emitting events
                    if (!isFirstRun || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STAY_SILENT_AT_FIRST_RUN")))
                    {
                        this.EmitEvents(serverName, this.files, newFiles);
                    }

                    // Updating our state
                    this.files = newFiles;
                }
            }
            catch (Exception ex)
            {
                this.error = ex.Message;
            }
        }

        protected abstract void EmitEvent(WhatHappenedEnum eventType, string filePath);

        private void EmitEvents(string serverName, IDictionary<string, DateTime> existingFiles, IDictionary<string, DateTime> newFiles)
        {
            foreach (var kv in existingFiles)
            {
                if (!newFiles.ContainsKey(kv.Key))
                {
                    this.EmitEvent(WhatHappenedEnum.FileRemoved, serverName + kv.Key);
                }
            }

            foreach (var kv in newFiles)
            {
                if (!existingFiles.ContainsKey(kv.Key))
                {
                    this.EmitEvent(WhatHappenedEnum.FileAdded, serverName + kv.Key);
                }
                else if (existingFiles[kv.Key] != kv.Value)
                {
                    this.EmitEvent(WhatHappenedEnum.FileModified, serverName + kv.Key);
                }
            }
        }

        private void GetParams(string folderFullPath,
            out string serverName,
            out string folderName,
            out string fileMask,
            out string userName,
            out string password)
        {
            int slashPos = folderFullPath.LastIndexOf('/');
            if (slashPos < 0)
            {
                serverName = folderFullPath;
                fileMask = "*.*";
            }
            else
            {
                serverName = folderFullPath.Substring(0, slashPos);
                fileMask = folderFullPath.Substring(slashPos + 1);
            }

            slashPos = serverName.IndexOf('/');
            if (slashPos < 0)
            {
                folderName = string.Empty;
            }
            else
            {
                folderName = serverName.Substring(slashPos + 1);
                serverName = serverName.Substring(0, slashPos);
            }

            dynamic folderParams = JObject.Parse(Environment.GetEnvironmentVariable("FOLDERS_TO_WATCH"))[folderFullPath];

            userName = ((JToken)folderParams).First.ToObject<JProperty>().Name;
            password = folderParams[userName];
            password = GetFromKeyVaultIfNeeded(password);
        }

        private static IDictionary<string, DateTime> ListFilesInFolder(SftpClient client, string folderName, Regex maskRegex)
        {
            var result = new Dictionary<string, DateTime>();

            foreach (var item in client.ListDirectory(folderName))
            {
                if (item.IsDirectory)
                {
                    if (item.Name == "." || item.Name == "..")
                    {
                        continue;
                    }

                    foreach (var kv in ListFilesInFolder(client, folderName + "/" + item.Name, maskRegex))
                    {
                        result[kv.Key] = kv.Value;
                    }
                }

                if (maskRegex.IsMatch(item.Name))
                {
                    result[item.FullName] = item.LastWriteTime;
                }
            }

            return result;
        }

        private const string KeyVaultUrlPart = ".vault.azure.net/secrets/";

        private static string GetFromKeyVaultIfNeeded(string secret)
        {
            if (!secret.Contains(KeyVaultUrlPart))
            {
                return secret;
            }

            string accessToken = new AzureServiceTokenProvider()
                .GetAccessTokenAsync("https://vault.azure.net").Result;

            // Taking the secret out of KeyVault
            using (var client = new WebClient())
            {
                client.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {accessToken}");
                string response = client.DownloadString($"{secret}?api-version=2016-10-01");
                
                return ((dynamic)JsonConvert.DeserializeObject(response)).value;
            }
        }
    }

    // Watcher entity's interface
    public interface ISftpWatcherEntity
    {
        void Watch(string folderFullPath);
    }

    public enum WhatHappenedEnum
    {
        FileAdded,
        FileRemoved,
        FileModified
    }
}