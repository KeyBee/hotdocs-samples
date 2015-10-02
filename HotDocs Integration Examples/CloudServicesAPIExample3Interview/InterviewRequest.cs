﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CloudServicesAPIExample3Interview
{
    public class InterviewRequest
    {
        public string GetInterviewFragment()
        {            
            // Get Interview Response from Cloud Services
            var interviewResponse = GetInterviewResponse();

            //Retrieve the multipart file stream containing interview files
            var interviewMultipartStream = GetMultipartStream(interviewResponse);

            // Extract the interview files
            var interviewFiles = GetInterviewFiles(interviewMultipartStream.Result);

            // Save the Template File JavaScript to the temp folder
            SaveInterviewFilesToTempDirectory(interviewFiles.Result);
            
            // Retrieve the Interview HTML fragment
            var interviewHtmlFragment = interviewFiles.Result["fragment.txt"];
            return interviewHtmlFragment;
        }                   

        public HttpResponseMessage GetInterviewResponse()
        {
            // Cloud Services Subscription Details
            string subscriberId = "example-subscriber-id";
            string signingKey = "example-signing-key";

            // HMAC calculation data      
            var timestamp = DateTime.UtcNow;
            var packageId = "HelloWorld";
            var format = "JavaScript";
            var templateName = "";
            var sendPackage = false;
            var billingRef = "";
            var tempImageUrl = "test";            
            var settings = new Dictionary<string, string>
            {
                {"HotDocsJsUrl", "https://cloud.hotdocs.ws/HDServerFiles/6.5/js/"},
                {"HotDocsCssUrl", "https://cloud.hotdocs.ws/HDServerFiles/6.5/stylesheets/hdsuser.css"}, 
                {"InterviewDefUrl", "http://localhost/CloudServicesAPIExample3Interview/Home/InterviewDefinition/"}, 
                {"SaveAnswersPageUrl", "http://localhost/examplehostapplication/save/"},                
                {"FormActionUrl", "InterviewFinish"}
            };

            // Generate HMAC using Cloud Services signing key            
            string hmac = CalculateHMAC(signingKey, timestamp, subscriberId, packageId, templateName, sendPackage, billingRef, format, tempImageUrl, settings);

            // Create assemble request            
            var request = CreateHttpRequestMessage(hmac, subscriberId, packageId, timestamp, format, tempImageUrl, settings);

            //Send assemble request to Cloud Services
            var client = new HttpClient();
            var response = client.SendAsync(request);            
            return response.Result;
        }

        static async Task<IEnumerable<HttpContent>> GetMultipartStream(HttpResponseMessage response)
        {
            IEnumerable<HttpContent> individualFileStreams = null;
            Task.Factory.StartNew(
                () => individualFileStreams = response.Content.ReadAsMultipartAsync().Result.Contents
            ).Wait();

            return individualFileStreams;
        }

        static async Task<Dictionary<string, string>> GetInterviewFiles(IEnumerable<HttpContent> multipartStream)
        {
            Dictionary<string, string> streams = new Dictionary<string, string>();
            foreach (var attachment in multipartStream)
            {
                var attachmentContent = await attachment.ReadAsStringAsync();
                var filename = attachment.Headers.ContentDisposition.FileName;
                streams.Add(filename, attachmentContent);
            }
            return streams;
        }

        private static void SaveInterviewFilesToTempDirectory(Dictionary<string, string> interviewFiles)
        {
            foreach (var file in interviewFiles)
            {
                var filePath = String.Format(@"C:\temp\{0}", file.Key);
                File.WriteAllText(filePath, file.Value);
            }
        } 

        private static HttpRequestMessage CreateHttpRequestMessage(string hmac, string subscriberId, string packageId, DateTime timestamp, string format, string tempImageUrl, Dictionary<string, string> settings)
        {
            var partialInterviewUrl = string.Format("https://cloud.hotdocs.ws/hdcs/interview/{0}/{1}?format={2}&tempimageurl={3}", subscriberId, packageId, format, tempImageUrl);
            var completedInterviewUrlBuilder = new StringBuilder(partialInterviewUrl);

            foreach (var kv in settings)
            {
                completedInterviewUrlBuilder.AppendFormat("&{0}={1}", kv.Key, kv.Value ?? "");
            }
            var InterviewUrl = completedInterviewUrlBuilder.ToString();

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(InterviewUrl),
                Method = HttpMethod.Post,
                Content = GetAnswers()
            };

            // Add request headers
            request.Headers.Add("x-hd-date", timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            request.Content.Headers.Remove("Content-Type");
            request.Content.Headers.Add("Content-Type", "text/xml");
            request.Headers.TryAddWithoutValidation("Authorization", hmac);
            request.Headers.Add("Keep-Alive", "false");

            return request;
        }

        private static StringContent GetAnswers()
        {
            return new StringContent(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><AnswerSet version=""1.1""><Answer name=""TextExample-t""><TextValue>Hello World</TextValue></Answer></AnswerSet >");
        }

        private static string CalculateHMAC(string signingKey, params object[] paramList)
        {
            byte[] key = Encoding.UTF8.GetBytes(signingKey);
            string stringToSign = CanonicalizeParameters(paramList);
            byte[] bytesToSign = Encoding.UTF8.GetBytes(stringToSign);
            byte[] signature;

            using (var hmac = new System.Security.Cryptography.HMACSHA1(key))
            {
                signature = hmac.ComputeHash(bytesToSign);
            }

            return Convert.ToBase64String(signature);
        }

        private static string CanonicalizeParameters(params object[] paramList)
        {
            if (paramList == null)
            {
                throw new ArgumentNullException();
            }

            var strings = paramList.Select(param =>
            {
                if (param is string || param is int || param is Enum || param is bool)
                {
                    return param.ToString();
                }

                if (param is DateTime)
                {
                    DateTime utcTime = ((DateTime) param).ToUniversalTime();
                    return utcTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
                }

                if (param is Dictionary<string, string>)
                {
                    var sorted = ((Dictionary<string, string>) param).OrderBy(kv => kv.Key);
                    var stringified = sorted.Select(kv => kv.Key + "=" + kv.Value).ToArray();
                    return string.Join("\n", stringified);
                }
                return "";
            });

            return string.Join("\n", strings.ToArray());
        }        
    }
}