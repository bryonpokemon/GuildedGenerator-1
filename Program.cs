using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using Microsoft.CSharp;
using Newtonsoft.Json.Linq;
using Microsoft.VisualBasic;
using System.Threading;

internal class Program
{
    public static string[] proxies = new string[] { };
    public static bool useProxies = false;
    public static int proxyIndex = 0;
    internal static readonly char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    public static string hmacs = "";

    static void Main()
    {
        Console.Title = "GuildedGenerator";

        System.Net.ServicePointManager.DefaultConnectionLimit = int.MaxValue;
        System.Net.ServicePointManager.MaxServicePoints = int.MaxValue;
        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        Console.WriteLine(@"

   _____       _ _     _          _  _____                           _             
  / ____|     (_) |   | |        | |/ ____|                         | |            
 | |  __ _   _ _| | __| | ___  __| | |  __  ___ _ __   ___ _ __ __ _| |_ ___  _ __ 
 | | |_ | | | | | |/ _` |/ _ \/ _` | | |_ |/ _ \ '_ \ / _ \ '__/ _` | __/ _ \| '__|
 | |__| | |_| | | | (_| |  __/ (_| | |__| |  __/ | | |  __/ | | (_| | || (_) | |   
  \_____|\__,_|_|_|\__,_|\___|\__,_|\_____|\___|_| |_|\___|_|  \__,_|\__\___/|_|   
                                                                                   
                                                                                   
   Made by ZioEren
");
        Logger.LogInfo("Welcome to GuildedGenerator, the first ever made Account Generator for Guilded.");

        if (!System.IO.File.Exists("proxies.txt"))
        {
            Logger.LogWarning("I can see that you have no loaded proxies, so I will not ask you if you wanna use proxies.");
        }
        else
        {
            Logger.LogWarning("I can see that you have a file with proxies in it, do you want to use them for generating? (y/n)");
            string answer = "";

            do
            {
                answer = Console.ReadLine();

                if (answer != "y" && answer != "n")
                {
                    Logger.LogError("Wrong answer! Answers allowed: y (yes) / n (no).");
                }
            }
            while (answer != "y" && answer != "n");

            if (answer == "y")
            {
                try
                {
                    proxies = System.IO.File.ReadAllLines("proxies.txt");
                }
                catch
                {
                    Logger.LogError("Failed to load your proxies file! Press ENTER to exit from the program.");
                    Console.ReadLine();
                    return;
                }

                if (proxies.Length == 0)
                {
                    Logger.LogError("You have loaded no proxies in your file! Press ENTER to exit from the program.");
                    Console.ReadLine();
                    return;
                }

                useProxies = true;
            }
        }

        while (true)
        {
            GenerateAccount(GetProxy(true), false, GenerateRandomString(17).ToLower() + "@esiix.com", GenerateRandomString(16), GenerateRandomString(20), GenerateRandomString(18), GenerateRandomString(22));
        }
    }

    public static WebProxy GetProxy(bool https)
    {
        try
        {
            try
            {
                if (proxyIndex >= proxies.Length)
                {
                    Interlocked.Decrement(ref proxyIndex);
                }

                Interlocked.Increment(ref proxyIndex);

                if (proxyIndex >= proxies.Length)
                {
                    Interlocked.Exchange(ref proxyIndex, 0);
                }

                string proxy = proxies[proxyIndex];
                int colons = 0;

                foreach (char c in proxy.ToCharArray())
                {
                    if (c == ':')
                    {
                        colons++;
                    }
                }

                if (colons == 0)
                {
                    return null;
                }

                string[] splitted = proxy.Split(':');

                if (colons == 1)
                {
                    if (https)
                    {
                        return new WebProxy($"https://{proxy}");
                    }
                    else
                    {
                        return new WebProxy($"http://{proxy}");
                    }
                }
                else if (colons == 3)
                {
                    WebProxy webProxy = null;

                    if (https)
                    {
                        webProxy = new WebProxy($"https://{proxy}");
                    }
                    else
                    {
                        webProxy = new WebProxy($"http://{proxy}");
                    }

                    webProxy.Credentials = new System.Net.NetworkCredential(splitted[2], splitted[3]);
                    return webProxy;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public static void GenerateAccount(WebProxy proxy, bool tried, string email, string username, string password, string biography = "", string tagLine = "")
    {
        try
        {
            Logger.LogWarning("Creating an account for Guilded, please wait a while...");

            var request = (HttpWebRequest)WebRequest.Create("https://www.guilded.gg/api/users?type=email");

            request.Proxy = proxy;
            request.UseDefaultCredentials = false;
            request.AllowAutoRedirect = false;

            var field = typeof(HttpWebRequest).GetField("_HttpRequestHeaders", BindingFlags.Instance | BindingFlags.NonPublic);

            request.Method = "POST";

            string content = "{\"extraInfo\":{\"platform\":\"desktop\"},\"name\":\"" + username + "\",\"email\":\"" + email + "\",\"password\":\"" + password + "\",\"fullName\":\"" + username + "\"}";

            byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(content);
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(requestBytes, 0, requestBytes.Length);
            requestStream.Close();

            var headers = new CustomWebHeaderCollection(new Dictionary<string, string>
            {
                ["Host"] = "www.guilded.gg",
                ["Content-Length"] = requestBytes.Length.ToString(),
                ["Content-Type"] = "application/json",
                ["guilded-device-id"] = GenerateRandomString(64).ToLower()
            });

            field.SetValue(request, headers);

            var response = request.GetResponse();
            dynamic jss = JObject.Parse(Encoding.UTF8.GetString(ReadFully(response.GetResponseStream())));
            string userId = jss.user.id;

            response.Close();
            response.Dispose();

            CompleteRegistration(username, email, password, userId, biography, tagLine);
        }
        catch
        {
            if (useProxies)
            {
                if (!tried)
                {
                    Logger.LogError("Failed to generate your account with HTTPS proxy. Trying with HTTP proxy.");
                    TryAgain(email, username, password, biography, tagLine);
                }
                else
                {
                    Logger.LogError("Failed to generate your Guilded account.");
                }
            }
            else
            {
                Logger.LogError("Failed to generate your Guilded account.");
            }
        }
    }

    public static void TryAgain(string email, string username, string password, string biography = "", string tagLine = "")
    {
        GenerateAccount(GetProxy(false), true, email, username, password, biography, tagLine);
    }

    public static void CompleteRegistration(string username, string email, string password, string userId, string biography = "", string tagLine = "")
    {
        try
        {
            var request = (HttpWebRequest)WebRequest.Create("https://www.guilded.gg/api/login");

            request.Proxy = null;
            request.UseDefaultCredentials = false;
            request.AllowAutoRedirect = false;

            var field = typeof(HttpWebRequest).GetField("_HttpRequestHeaders", BindingFlags.Instance | BindingFlags.NonPublic);

            request.Method = "POST";

            string content = "{\"getMe\":true,\"email\":\"" + email + "\",\"password\":\"" + password + "\"}";

            byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(content);
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(requestBytes, 0, requestBytes.Length);
            requestStream.Close();

            var headers = new CustomWebHeaderCollection(new Dictionary<string, string>
            {
                ["Host"] = "www.guilded.gg",
                ["Content-Length"] = requestBytes.Length.ToString(),
                ["Content-Type"] = "application/json"
            });

            field.SetValue(request, headers);

            var response = request.GetResponse();
            string hmac_signed_session = "";

            foreach (string header in response.Headers.GetValues("Set-Cookie"))
            {
                if (header.StartsWith("hmac_signed_session"))
                {
                    hmac_signed_session = header.Replace("hmac_signed_session=", "").Split(';')[0];
                    break;
                }
            }

            response.Close();
            response.Dispose();

            SetAboutInformations(hmac_signed_session, userId, email, biography, tagLine);
        }
        catch
        {
            Logger.LogError("Failed to generate your Guilded account.");
        }
    }

    public static void SetAboutInformations(string hmac, string userId, string email, string biography = "", string tagLine = "")
    {
        try
        {
            if (biography == "" && tagLine == "")
            {
                return;
            }

            var request = (HttpWebRequest)WebRequest.Create($"https://www.guilded.gg/api/users/" + userId + "/profilev2");

            request.Proxy = null;
            request.UseDefaultCredentials = false;
            request.AllowAutoRedirect = false;

            var field = typeof(HttpWebRequest).GetField("_HttpRequestHeaders", BindingFlags.Instance | BindingFlags.NonPublic);

            request.Method = "PUT";

            string content = "";

            if (biography != "" && tagLine != "")
            {
                content = "{\"userId\":\"" + userId + "\",\"aboutInfo\":{\"bio\":\"" + biography + "\",\"tagLine\":\"" + tagLine + "\"}}";
            }
            else if (biography != "")
            {
                content = "{\"userId\":\"" + userId + "\",\"aboutInfo\":{\"bio\":\"" + biography + "\"}}";
            }
            else
            {
                content = "{\"userId\":\"" + userId + "\",\"aboutInfo\":{\"tagLine\":\"" + tagLine + "\"}}";
            }

            byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(content);
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(requestBytes, 0, requestBytes.Length);
            requestStream.Close();

            var headers = new CustomWebHeaderCollection(new Dictionary<string, string>
            {
                ["Host"] = "www.guilded.gg",
                ["Content-Length"] = requestBytes.Length.ToString(),
                ["Content-Type"] = "application/json",
                ["Cookie"] = $"hmac_signed_session={hmac}"
            });

            field.SetValue(request, headers);

            var response = request.GetResponse();

            response.Close();
            response.Dispose();

            DoEmailVerification(hmac, userId, email);
        }
        catch
        {
            Logger.LogError("Failed to generate your Guilded account.");
        }
    }

    public static void DoEmailVerification(string hmac, string userId, string email)
    {
        try
        {
            var request = (HttpWebRequest)WebRequest.Create($"https://www.guilded.gg/api/email/verify");

            request.Proxy = null;
            request.UseDefaultCredentials = false;
            request.AllowAutoRedirect = false;

            var field = typeof(HttpWebRequest).GetField("_HttpRequestHeaders", BindingFlags.Instance | BindingFlags.NonPublic);

            request.Method = "POST";

            var headers = new CustomWebHeaderCollection(new Dictionary<string, string>
            {
                ["Host"] = "www.guilded.gg",
                ["Cookie"] = $"hmac_signed_session={hmac}"
            });

            field.SetValue(request, headers);

            var response = request.GetResponse();

            response.Close();
            response.Dispose();

            EmailVerify(hmac, userId, email);
        }
        catch
        {
            Logger.LogError("Failed to generate your Guilded account.");
        }
    }

    public static void EmailVerify(string hmac, string userId, string email)
    {
        try
        {
            string emailName = email.Split('@')[0], domain = email.Split('@')[1], messageId = "", emailContent = "";

            {
                while (messageId == "")
                {
                    Thread.Sleep(1250);

                    try
                    {
                        var request = (HttpWebRequest)WebRequest.Create($"https://www.1secmail.com/api/v1/?action=getMessages&login=" + emailName + "&domain=" + domain);

                        request.Proxy = null;
                        request.UseDefaultCredentials = false;
                        request.AllowAutoRedirect = false;

                        var field = typeof(HttpWebRequest).GetField("_HttpRequestHeaders", BindingFlags.Instance | BindingFlags.NonPublic);

                        request.Method = "GET";

                        var headers = new CustomWebHeaderCollection(new Dictionary<string, string>
                        {
                            ["Host"] = "www.1secmail.com",
                        });

                        field.SetValue(request, headers);

                        var response = request.GetResponse();
                        string result = Encoding.UTF8.GetString(ReadFully(response.GetResponseStream()));
                        string[] splitted1 = Strings.Split(result, "\"id\":");
                        messageId = splitted1[1].Split(',')[0];

                        response.Close();
                        response.Dispose();
                    }
                    catch
                    {

                    }
                }
            }

            {
                var request = (HttpWebRequest)WebRequest.Create($"https://www.1secmail.com/api/v1/?action=readMessage&login=" + emailName + "&domain=" + domain + "&id=" + messageId);

                request.Proxy = null;
                request.UseDefaultCredentials = false;
                request.AllowAutoRedirect = false;

                var field = typeof(HttpWebRequest).GetField("_HttpRequestHeaders", BindingFlags.Instance | BindingFlags.NonPublic);

                request.Method = "GET";

                var headers = new CustomWebHeaderCollection(new Dictionary<string, string>
                {
                    ["Host"] = "www.1secmail.com",
                });

                field.SetValue(request, headers);

                var response = request.GetResponse();
                emailContent = Encoding.UTF8.GetString(ReadFully(response.GetResponseStream()));

                response.Close();
                response.Dispose();
            }

            string[] splitted = Strings.Split(emailContent, "verify?token=");
            string token = splitted[1];
            string[] another = Strings.Split(token, "\\" + "\"");
            token = another[0];

            {
                var request = (HttpWebRequest)WebRequest.Create($"https://www.guilded.gg/api/email/verify?token=" + token);

                request.Proxy = null;
                request.UseDefaultCredentials = false;
                request.AllowAutoRedirect = false;

                var field = typeof(HttpWebRequest).GetField("_HttpRequestHeaders", BindingFlags.Instance | BindingFlags.NonPublic);

                request.Method = "GET";

                var headers = new CustomWebHeaderCollection(new Dictionary<string, string>
                {
                    ["Host"] = "www.guilded.gg",
                    ["Cookie"] = $"hmac_signed_session={hmac}"
                });

                field.SetValue(request, headers);

                var response = request.GetResponse();

                response.Close();
                response.Dispose();
            }

            if (!System.IO.File.Exists("hmacs.txt"))
            {
                System.IO.File.WriteAllText("hmacs.txt", hmac);
            }
            else
            {
                System.IO.File.AppendAllText("hmacs.txt", $"\r\n{hmac}");
            }

            Logger.LogInfo($"Succesfully generated your Guilded account (HMAC): {hmac}. User ID: {userId}.");
        }
        catch
        {
            Logger.LogError("Failed to generate your Guilded account.");
        }
    }

    public static byte[] ReadFully(Stream input)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            input.CopyTo(ms);
            return ms.ToArray();
        }
    }

    public static string GenerateRandomString(int size)
    {
        try
        {
            byte[] data = new byte[4 * size];

            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(data);
            }

            StringBuilder result = new StringBuilder(size);

            for (int i = 0; i < size; i++)
            {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % chars.Length;

                result.Append(chars[idx]);
            }

            return result.ToString();
        }
        catch
        {
            return "";
        }
    }
}