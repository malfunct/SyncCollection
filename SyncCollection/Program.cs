using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SyncCollection
{
    class Program
    {
        const string Collection = "apple_ii_library_4am";
        const string Rows = "2000";

        static void Main(string[] args)
        {
            string collection = Collection;
            if (args.Length > 0)
            {
                collection = args[0];
            }
            else
            {
                Console.WriteLine($"Using default collection {collection}");
            }

            var task = MainAsync(collection);
            task.Wait();
        }

        static async Task MainAsync(string collection)
        {
            var searchResults = await GetSearchResults(collection);

            var localFileList = GetListOfAlreadyDownloadedFiles(collection);

            DownloadFiles(searchResults, localFileList, collection);
        }

        private static void ArchiveOldDownloadList(string collection)
        {
            string fileListPath = Path.Combine(collection, "fileList.txt");
            string filePathOldList = Path.Combine(collection, "fileListOld.txt");

            if (File.Exists(fileListPath))
            {
                if (File.Exists(filePathOldList))
                {
                    File.Delete(filePathOldList);
                }

                File.Move(fileListPath, filePathOldList);
            }
        }

        private static void DownloadFiles(Dictionary<string, DateTime> searchResults,
            Dictionary<string, DateTime> localFileList, string collection)
        {
            ArchiveOldDownloadList(collection);

            Dictionary<string, DateTime> updatedFileList = new Dictionary<string, DateTime>(localFileList);
            string currentlyDownloading = null;

            try
            {
                string resourceBase = "https://archive.org/compress";
                WebClient client = new WebClient();

                foreach (var indicatorToDownload in searchResults.Keys)
                {
                    if (!localFileList.ContainsKey(indicatorToDownload) || searchResults[indicatorToDownload] >
                        localFileList[indicatorToDownload])
                    {
                        currentlyDownloading = $"{collection}/{indicatorToDownload}.zip";

                        var url = $"{resourceBase}/{indicatorToDownload}";

                        Console.WriteLine("Downloading {0}", currentlyDownloading);
                        Console.WriteLine("Downloading from {0}", url);

                        bool success = false;
                        try
                        {
                            client.DownloadFile(url, currentlyDownloading);
                            success = true;
                        }
                        catch (WebException e)
                        {
                            // Just skip webexceptions and clean up so we can
                            // download as much of the collection as possible
                            Console.WriteLine("Error while downloading {0}", e.Message);

                            // delete failed download
                            if (File.Exists(currentlyDownloading))
                            {
                                File.Delete(currentlyDownloading);
                            }

                            UpdateLocalFileList(updatedFileList, collection);
                        }

                        if (success)
                        {
                            currentlyDownloading = null;
                            updatedFileList[indicatorToDownload] = searchResults[indicatorToDownload];

                            UpdateLocalFileList(updatedFileList, collection); //we churn this file a lot so we don't lose much state if process interrupted
                        }
                        System.Threading.Thread.Sleep(500); //forcing a sleep to be nice to archive.org?
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("Error while downloading {0}", exc.Message);
            }
            finally
            {
                // delete failed download
                if (currentlyDownloading != null && File.Exists(currentlyDownloading))
                {
                    File.Delete(currentlyDownloading);
                }

                UpdateLocalFileList(updatedFileList, collection);
            }
        }

        private static void UpdateLocalFileList(Dictionary<string, DateTime> updatedFileList, string collection)
        {
            var filePath = Path.Combine(collection, "fileList.txt");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (var fileWriter = new StreamWriter(filePath))
            {
                foreach (var key in updatedFileList.Keys)
                {
                    fileWriter.WriteLine($"{key}\t{updatedFileList[key]}");
                }
            }
        }

        private static Dictionary<string, DateTime> GetListOfAlreadyDownloadedFiles(string collection)
        {
            Dictionary<string, DateTime> localFileList = new Dictionary<string, DateTime>(5000);

            if (!Directory.Exists(collection))
            {
                Directory.CreateDirectory(collection);
            }

            string fileListPath = Path.Combine(collection, "fileList.txt");

            if (File.Exists(fileListPath))
            {
                foreach (var line in File.ReadAllLines(fileListPath))
                {
                    var split = line.Split('\t');
                    localFileList[split[0]] = DateTime.Parse(split[1]);
                }
            }
            return localFileList;
        }

        private static async Task<Dictionary<string, DateTime>> GetSearchResults(string collection)
        {
            var url =
                $"https://archive.org/advancedsearch.php?q=collection%3A{collection}&fl%5B%5D=identifier&fl%5B%5D=oai_updatedate&sort%5B%5D=identifier+asc&sort%5B%5D=&sort%5B%5D=&rows={Rows}&page=1&output=json";

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Request for collection list failed with the error: ({0}) {1}", response.StatusCode,
                    response.ReasonPhrase);
                throw new Exception("Request Failed");
            }

            var jsonResult = await response.Content.ReadAsStringAsync();

            var searchResult = JsonConvert.DeserializeObject<InternetArchiveSearchResult>(jsonResult);

            Dictionary<string, DateTime> searchResultPairs = new Dictionary<string, DateTime>(5000);

            foreach (var docDescriptor in searchResult.response.docs)
            {
                searchResultPairs[docDescriptor.identifier] =
                    docDescriptor.oai_updatedate.Last(); //Last element in oai_updatedate is the "Updated" date
            }
            return searchResultPairs;
        }
    }
}