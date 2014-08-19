using RalExtractorByDateRange.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace RalExtractorByDateRange
{
    class Program
    {
        private static string _token;
        private static string _rawRal;
        private static string _rawTranscript;
        private static List<Ral> _rals;
        private static List<SimpleTranscript> _transcripts;
        private static Uri _baseAddress;
        private static int _timeout = 50000;
        private static int _waitTime = 500;
        private static bool error;

        static int Main(string[] args)
        {
            //the worst thing I can do... wrap this in a huge try-catch
            try
            {
                _baseAddress = new Uri(ConfigurationManager.AppSettings["callminer-api-address"]);

                return TranscriptStuff(ref args);

                //return RalStuff(ref args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("something bad happened... message:{0} stack:{1}", ex.Message, ex.StackTrace));
                return -1;
            }
        }

        private static int TranscriptStuff(ref string[] args)
        {
#if(DEBUG)
            args = new[] { "2884774,2884759,2884761,2884760,2884762,2884763,2884765,2884764,2884766,2884767,2884770,2884768,2884770,2884769,2884771,2884772,2884773" };
#endif
            Task t = GetToken();

            _transcripts = new List<SimpleTranscript>();

            if (t.Wait(_timeout))
            {
                Thread.Sleep(_waitTime);

                if (!string.IsNullOrWhiteSpace(_token))
                {
                    //Get Transcripts
                    foreach (var i in args[0].Split(','))
                    {
                        Console.WriteLine();
                        Console.WriteLine("getting Transcript for this pass");
                        Task rt = GetTranscript(i);
                        if (rt.Wait(_timeout))
                        {
                            Thread.Sleep(_waitTime);
                            if (!string.IsNullOrWhiteSpace(_rawTranscript))
                            {
                                var newTranscriptStuff = JsonConvert.DeserializeObject<List<SimpleTranscript>>(_rawTranscript, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects });

                                if (newTranscriptStuff == null)
                                {
                                    //do nothing just keep going
                                }
                                else
                                {
                                    Console.WriteLine("adding " + newTranscriptStuff.Count);
                                     _transcripts.AddRange(newTranscriptStuff);
                                    Console.WriteLine("total record count " + _transcripts.Count);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine(string.Format("getting transcript for {0} failed", i));
                            break;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Get Token Timed Out");
            }
            WriteTranscriptToFile("sample");
            Console.ReadLine();
            return 1;
        }

        private static int RalStuff(ref string[] args)
        {
            string startingDate;
            string endingDate;

#if(!DEBUG)
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
#elif(DEBUG)
            args = new[] { "range-4/02/2014-4/03/2014" };
#endif

            string[] parts;
            ParseArgs(args, out startingDate, out endingDate, out parts);

            if (error)
            {
                return -1;
            }

            _rals = new List<Ral>();

            Task t = GetToken();

            if (t.Wait(_timeout))
            {
                Thread.Sleep(_waitTime);

                if (!string.IsNullOrWhiteSpace(_token))
                {
                    //Get RAL Data by Date Range
                    var maxPages = 10000000000;
                    for (int i = 1; i < maxPages; i++)
                    {
                        Console.WriteLine();
                        Console.WriteLine("getting RAL data for this pass");
                        Task rt = GetRalData(i, startingDate, endingDate);
                        if (rt.Wait(_timeout))
                        {
                            Thread.Sleep(_waitTime);
                            if (!string.IsNullOrWhiteSpace(_rawRal))
                            {
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
                        }
                        else
                        {
                            Console.WriteLine(string.Format("getting RAL data for page {0} between {1} and {2} failed", i, startingDate, endingDate));
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
            startingDate = DateTime.Today.ToShortDateString();
            endingDate = DateTime.Today.ToShortDateString();

            if (parts[0] == "today")
            {
                startingDate = DateTime.Today.AddDays(-1).ToShortDateString();
                endingDate = DateTime.Today.ToShortDateString();
            }
            else if (parts[0] == "yesterday")
            {
                startingDate = DateTime.Today.AddDays(-2).ToShortDateString();
                endingDate = DateTime.Today.AddDays(-1).ToShortDateString();
            }
            else if (parts[0] == "twodaysago")
            {
                startingDate = DateTime.Today.AddDays(-3).ToShortDateString();
                endingDate = DateTime.Today.AddDays(-2).ToShortDateString();
            }
            else if (parts[0] == "range")
            {
                DateTime sDate;
                DateTime.TryParse(parts[1], out sDate);
                if (sDate != null)
                {
                    startingDate = sDate.ToShortDateString();
                }
                else
                {
                    Console.WriteLine("bad starting date format");
                    error = true;
                }

                DateTime eDate;
                DateTime.TryParse(parts[2], out eDate);
                if (eDate != null)
                {
                    endingDate = eDate.ToShortDateString();
                }
                else
                {
                    Console.WriteLine("bad ending date format");
                    error = true;
                }
            }
            else
            {
                startingDate = DateTime.Today.AddDays(-1).ToShortDateString();
                endingDate = DateTime.Today.ToShortDateString();
            }
        }

        private static void WriteRalStuffToFile(int i)
        {
            var fullPayload = JsonConvert.SerializeObject(_rals, Formatting.Indented);
            var fileName = string.Format("raloutput_{0}.json", i);
            Console.WriteLine();
            Console.WriteLine(string.Format("writing file {0} containing 100 records", fileName));
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                writer.Write(fullPayload);
            }
        }

        private static void WriteTranscriptToFile(string i)
        {
            var fullPayload = JsonConvert.SerializeObject(_transcripts, Formatting.Indented);
            var fileName = string.Format("transcriptoutput_{0}.json", i);
            Console.WriteLine();
            Console.WriteLine(string.Format("writing file {0}", fileName));
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                writer.Write(fullPayload);
            }
        }

        private static Task GetToken(FormUrlEncodedContent postData)
        {
            Task t = GetToken(_baseAddress, Helper.SecurityApiPart, postData);
            t.ContinueWith((str) =>
            {
                _token = (str as Task<string>).Result;

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
            int numRec = 20; //number of records
            var request = string.Format(Helper.ContactsByDateRangeApiPart, startDate, endDate, numRec, pageNumber);

            Task tr = Run(_baseAddress, request);
            tr.ContinueWith((ralStr) =>
            {
                _rawRal = (ralStr as Task<string>).Result;
                Console.WriteLine(string.Format("got {0} raw data for page {1} between {2} and {3}", 20, pageNumber, startDate, endDate));
            });

            return tr;
        }

        private static Task GetTranscript(string contactId)
        {
            var request = string.Format(Helper.TranscriptApiPart, contactId);

            Task tr = Run(_baseAddress, request);
            tr.ContinueWith((tranStr) =>
            {
                _rawTranscript = (tranStr as Task<string>).Result;
                Console.WriteLine(string.Format("got transcript for {0}", contactId));
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
                    HttpResponseMessage response = await client.PostAsync(requestUri, postData);

                    // Check that the response was successful or throw an exception
                    response.EnsureSuccessStatusCode();

                    // Read the response to get the token
                    string responseBody = await response.Content.ReadAsStringAsync();
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
                using (var client = new HttpClient { BaseAddress = baseAddress })
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("JWT", _token.Replace("\"", ""));

                    var response = await client.GetAsync(requestUri);

                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();

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
