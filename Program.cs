using RalExtractorByDateRange.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace RalExtractorByDateRange
{
    class Program
    {
        private static string _token;
        private static string _rawRal;
        private static List<Ral> _rals;
        private static Uri _baseAddress;
        private const int Timeout = 50000;
        private const int WaitTime = 500;

        static int Main(string[] args)
        {
            //the worst thing I can do... wrap this in a huge try-catch
            try
            {
                _baseAddress = new Uri(ConfigurationManager.AppSettings["callminer-api-address"]);
#if(!DEBUG)
                return RalStuff(ref args);
#elif(DEBUG)
                return RalStuff();
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine("something bad happened... message:{0} stack:{1}", ex.Message, ex.StackTrace);
                return -1;
            }
        }

        private static int RalStuff()
        {
            var args = new[] { "range-10/1/2014-10/30/2014" };
            return RalStuff(ref args);
        }

        private static int RalStuff(ref string[] args)
        {
            string startingDate;
            string endingDate;

            if (args.Length == 0)
            {
                Console.WriteLine("Please enter one of the following");
                Console.WriteLine("today");
                Console.WriteLine("yesterday");
                Console.WriteLine("twodaysago");
                Console.WriteLine("range-<start>-<end>");
                Console.WriteLine("Usage: RalExtractorByDateRange <value>");
                return 1;
            }

            string[] parts;
            ParseArgs(args, out startingDate, out endingDate, out parts);

            _rals = new List<Ral>();

            var t = GetToken();

            if (t.Wait(Timeout))
            {
                Thread.Sleep(WaitTime);

                if (!string.IsNullOrWhiteSpace(_token))
                {
                    //Get RAL Data by Date Range
                    const long maxPages = 10000000000;
                    for (var i = 1; i < maxPages; i++)
                    {
                        Console.WriteLine();
                        Console.WriteLine("getting RAL data for this pass");
                        var rt = GetRalData(i, startingDate, endingDate);
                        if (rt.Wait(Timeout))
                        {
                            Thread.Sleep(WaitTime);
                            if (string.IsNullOrWhiteSpace(_rawRal))
                                continue;

                            var newRalStuff = JsonConvert.DeserializeObject<List<Ral>>(_rawRal, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects });

                            if (newRalStuff == null)
                            {
                                //do nothing just keep going
                            }
                            else if (newRalStuff.Count == 0)
                            {
                                Console.WriteLine("no new records to add, ending");
                                WriteRalStuffToFile(i);
                                break;
                            }
                            else
                            {
                                Console.WriteLine("adding " + newRalStuff.Count);
                                _rals.AddRange(newRalStuff);
                                Console.WriteLine("total record count " + _rals.Count);
                            }

                            if (_rals.Count >= 100)
                            {
                                WriteRalStuffToFile(i);

                                Console.WriteLine("clearing internal storage of RAL data");
                                _rals.Clear();
                            }
                        }
                        else
                        {
                            Console.WriteLine("getting RAL data for page {0} between {1} and {2} failed", i, startingDate, endingDate);
                            break;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Get Token Timed Out");
            }

            Console.ReadLine();
            return 1;
        }

        private static Task GetToken()
        {
            var pairs = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("apiKey", ConfigurationManager.AppSettings["callminer-api-key"]),
                    new KeyValuePair<string, string>("username", ConfigurationManager.AppSettings["callminer-api-username"]),
                    new KeyValuePair<string, string>("password", ConfigurationManager.AppSettings["callminer-api-password"])
                };

            var postData = new FormUrlEncodedContent(pairs);

            Console.WriteLine("getting Token");
            Task t = GetToken(postData);
            return t;
        }

        private static void ParseArgs(string[] args, out string startingDate, out string endingDate, out string[] parts)
        {
            parts = args[0].Split('-');

            switch (parts[0])
            {
                case "today":
                    startingDate = DateTime.Today.AddDays(-1).ToShortDateString();
                    endingDate = DateTime.Today.ToShortDateString();
                    break;
                case "yesterday":
                    startingDate = DateTime.Today.AddDays(-2).ToShortDateString();
                    endingDate = DateTime.Today.AddDays(-1).ToShortDateString();
                    break;
                case "twodaysago":
                    startingDate = DateTime.Today.AddDays(-3).ToShortDateString();
                    endingDate = DateTime.Today.AddDays(-2).ToShortDateString();
                    break;
                case "range":
                    {
                        DateTime sDate;
                        DateTime.TryParse(parts[1], out sDate);
                        startingDate = sDate.ToShortDateString();

                        DateTime eDate;
                        DateTime.TryParse(parts[2], out eDate);
                        endingDate = eDate.ToShortDateString();
                    }
                    break;
                default:
                    startingDate = DateTime.Today.AddDays(-1).ToShortDateString();
                    endingDate = DateTime.Today.ToShortDateString();
                    break;
            }
        }

        private static void WriteRalStuffToFile(int i)
        {
            var fullPayload = JsonConvert.SerializeObject(_rals, Formatting.Indented);
            var fileName = string.Format("raloutput_{0}.json", i);
            Console.WriteLine();
            Console.WriteLine("writing file {0} containing 100 records", fileName);
            using (var writer = new StreamWriter(fileName))
            {
                writer.Write(fullPayload);
            }
        }

        private static Task GetToken(HttpContent postData)
        {
            Task t = GetToken(_baseAddress, "security/getToken", postData);
            t.ContinueWith(str =>
            {
                var task = str as Task<string>;
                if (task != null)
                    _token = task.Result;

                if (string.IsNullOrWhiteSpace(_token))
                {
                    Console.WriteLine("Token was not populated");
                }
                Console.WriteLine("got Token");

            });
            return t;
        }

        private static Task GetRalData(int pageNumber, string startDate, string endDate)
        {
            //Get RAL Data by Date Range
            const int numRec = 20; //number of records
            var request = string.Format("/ral/datesearch?startDate={0}&stopDate={1}&records={2}&page={3}", startDate, endDate, numRec, pageNumber);

            Task tr = Run(_baseAddress, request);
            tr.ContinueWith(ralStr =>
            {
                _rawRal = (ralStr as Task<string>).Result;
                Console.WriteLine("got {0} raw data for page {1} between {2} and {3}", 20, pageNumber, startDate, endDate);
            });

            return tr;
        }

        private static async Task<string> GetToken(Uri baseAddress, string requestUri, HttpContent postData)
        {
            try
            {
                // Create a HttpClient instance
                using (var client = new HttpClient { BaseAddress = baseAddress })
                {
                    // Send a request asynchronously, continue when complete
                    var response = await client.PostAsync(requestUri, postData);

                    // Check that the response was successful or throw an exception
                    response.EnsureSuccessStatusCode();

                    // Read the response to get the token
                    var responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex.Message + @" " + ex.InnerException);
                throw;
            }
        }

        private static async Task<string> Run(Uri baseAddress, string requestUri)
        {
            try
            {
                // Create a HttpClient instance
                using (var client = new HttpClient { BaseAddress = baseAddress })
                {
                    // The token that was created during login
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("JWT",
                        _token.Replace("\"", ""));

                    // Send a request asynchronously, continue when complete
                    var response = await client.GetAsync(requestUri);

                    // Check that the response was successful or throw an exception
                    response.EnsureSuccessStatusCode();

                    // Read the response to get the information
                    var responseBody = await response.Content.ReadAsStringAsync();

                    // Replace the current token with a new token provided via the API
                    if (response.Headers.Contains("auth-token-updated"))
                    {
                        _token = response.Headers.GetValues("auth-token-updated").FirstOrDefault() ?? _token;
                    }
                    return responseBody;

                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex.Message + @" " + ex.InnerException);
                throw;
            }
        }
    }
}
