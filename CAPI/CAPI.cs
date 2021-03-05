﻿/*
 * Original Code from EDDI https://github.com/EDCD/EDDI Thanks for the EDDI team for this
 * 
 * Modified code Copyright © 2021 Robby & EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using BaseUtils.JSON;

namespace CAPI
{
    public class CompanionAPI
    {
        public enum State
        {
            LoggedOut,
            AwaitingCallback,
            Authorized,
            TokenRefresh,
        };

        public State CurrentState { get; set; } = State.LoggedOut;
        public CompanionAppCredentials Credentials;
        public bool Active => CurrentState == State.Authorized;
        public bool LoggedOut => CurrentState == State.LoggedOut;
        public bool GameIsBeta { get; private set; }
        public string User { get; private set; }

        public CompanionAPI(string credentialpath, string clientinfo, string useragent, string uri)
        {
            this.credentialpath = credentialpath;
            this.clientID = clientinfo;
            this.uri = uri;
            this.useragent = useragent;
        }

        // is the user credential file present, and if so, has it tokens so its been logged in

        public bool IsCredentialFilePresent(string username, out bool isloggedin)
        {
            isloggedin = false;
            string credfile = Path.Combine(credentialpath, SafeFileString(username) + ".cred");
            if (File.Exists(credfile))
            {
                var credentials = CompanionAppCredentials.Load(credfile);
                isloggedin = credentials.IsAccessRefreshTokenPresent;
                return true;
            }
            else
                return false;
        }

        // login for user, we can login over another user
        // returns true if operating okay (may be performing user login) or false if can't log in

        public bool Login(string username, bool gameisbeta = false)           
        {
            if (clientID == null && clientID.Length>0)    // must have an ID, else service is disabled
                return false;

            if (username == User && CurrentState != State.LoggedOut && GameIsBeta == gameisbeta)     // if logged in to the same user
                return true;

            string credfile = Path.Combine(credentialpath, SafeFileString(username) + ".cred");
            Credentials = CompanionAppCredentials.Load(credfile);

            GameIsBeta = gameisbeta;
            User = username;

            CurrentState = State.LoggedOut;         // clear state back to logged out
            cachedProfile = null;                   // empty any cached profile

            try
            {
                RefreshToken();
            }
            catch ( EliteDangerousCompanionWebException ws)                 // web exceptions, they happen. don't lost the refresh token over it
            {
                System.Diagnostics.Debug.WriteLine(ws);
                return false;
            }
            catch (Exception)
            {
                AskForLogin();          // and login
            }

            return true;        // everything is ok, either we have logged in, or we are in the process of asking the user for a login
        }

        // Log out of the companion API and remove local credentials
        public void Logout()
        {
            Credentials.Clear();
            Credentials.Save();
            CurrentState = State.LoggedOut;
            cachedProfile = null;
        }

        // throws web exception, or AuthenticationException (logs you out).  Else its got the access token and your good to go

        private void RefreshToken()  // may throw
        {
            if (Credentials.refreshToken == null)
            {
                Logout();
                throw new EliteDangerousCompanionAppAuthenticationException("Refresh token not found, need full login");
            }

            CurrentState = State.TokenRefresh;

            HttpWebRequest request = GetRequest(AUTH_SERVER + TOKEN_URL);
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            byte[] data = Encoding.UTF8.GetBytes($"grant_type=refresh_token&client_id={clientID}&refresh_token={Credentials.refreshToken}");
            request.ContentLength = data.Length;
            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(data, 0, data.Length);
            }

            using (HttpWebResponse response = GetResponse(request))
            {
                if (response == null)
                {
                    throw new EliteDangerousCompanionWebException("Failed to contact API server");
                }

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseData = getResponseData(response);
                    JObject json = JObject.Parse(responseData);
                    Credentials.refreshToken = (string)json["refresh_token"];
                    Credentials.accessToken = (string)json["access_token"];
                    Credentials.tokenExpiry = DateTime.UtcNow.AddSeconds((double)json["expires_in"]);
                    Credentials.Save();

                    if (Credentials.accessToken == null)
                    {
                        Logout();
                        throw new EliteDangerousCompanionAppAuthenticationException("Access token not found");
                    }

                    CurrentState = State.Authorized;
                }
                else
                {
                    Logout();
                    throw new EliteDangerousCompanionAppAuthenticationException("Invalid refresh token");
                }
            }
        }

        private void AskForLogin()      
        {
            CurrentState = State.AwaitingCallback;

            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

            byte[] rawVerifier = new byte[32];
            rng.GetBytes(rawVerifier);
            verifier = base64UrlEncode(rawVerifier);                        // Create the verifier random key

            byte[] rawAuthSessionID = new byte[8];
            rng.GetBytes(rawAuthSessionID);
            authSessionID = base64UrlEncode(rawAuthSessionID);              // and a random auth session id

            byte[] byteVerifier = Encoding.ASCII.GetBytes(verifier);
            byte[] hash = SHA256.Create().ComputeHash(byteVerifier);
            string codeChallenge = base64UrlEncode(hash);                   // create a SHA256 version of the verifier

                                                                            // issue the request to frontier with the required audience, scope, client id
                                                                            // code challenge, authsessioID and the URI to call back on
            string CALLBACK_URL = $"{uri}://auth/";
            string webURL = $"{AUTH_SERVER}{AUTH_URL}" + $"?response_type=code&{AUDIENCE}&{SCOPE}&client_id={clientID}&code_challenge={codeChallenge}&code_challenge_method=S256&state={authSessionID}&redirect_uri={Uri.EscapeDataString(CALLBACK_URL)}";
            Process.Start(webURL);
        }

        // DDE server calls this with the URL callback
        // NB any user can send an arbitrary URL from the Windows Run dialog, so it must be treated as untrusted

        public void URLCallBack(string url)
        {
            try
            {
                string CALLBACK_URL = $"{uri}://auth/";

                // verify the callback url is ours, and contains parameters

                if (!(url.StartsWith(CALLBACK_URL) && url.Contains("?")))
                {
                    throw new EliteDangerousCompanionAppAuthenticationException("Malformed callback URL from Frontier");
                }

                // parse out the query string into key / value pairs

                Dictionary<string, string> paramsDict = ParseQueryString(url);

                // we need an authsessionID, we need a state response, and it must be the same authsessionID

                if (authSessionID == null || !paramsDict.ContainsKey("state") || paramsDict["state"] != authSessionID)
                {
                    throw new EliteDangerousCompanionAppAuthenticationException("Unexpected callback URL from Frontier");
                }

                authSessionID = null;       // for security, throw it away so noone can use it again (in case we get a double URL call back for some reason)

                // must have a code, if not, we are borked

                if (!paramsDict.ContainsKey("code"))
                {
                    if (!paramsDict.TryGetValue("error_description", out string desc))
                    {
                        paramsDict.TryGetValue("error", out desc);
                    }

                    desc = desc ?? "no error description";
                    throw new EliteDangerousCompanionAppAuthenticationException($"Negative response from Frontier: {desc}");
                }

                string code = paramsDict["code"];

                // now request frontier with the TOKEN URL to get the access token

                HttpWebRequest request = GetRequest(AUTH_SERVER + TOKEN_URL);
                request.ContentType = "application/x-www-form-urlencoded";
                request.Method = "POST";
                request.KeepAlive = false;
                request.AllowAutoRedirect = true;

                // we send back the clientID, verifier, code and the requesturi

                byte[] data = Encoding.UTF8.GetBytes($"grant_type=authorization_code&client_id={clientID}&code_verifier={verifier}&code={code}&redirect_uri={Uri.EscapeDataString(CALLBACK_URL)}");
                request.ContentLength = data.Length;

                // in the request stream, we write the data above
                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(data, 0, data.Length);
                }

                // now get the response
                using (HttpWebResponse response = GetResponse(request))
                {
                    if (response?.StatusCode == null)
                    {
                        throw new EliteDangerousCompanionAppAuthenticationException("Failed to contact authorization server");
                    }
                    else if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseData = getResponseData(response);
                        JObject json = JObject.Parse(responseData);

                        // store the refresh token, access token and expiry time into the credential file

                        Credentials.refreshToken = (string)json["refresh_token"];
                        Credentials.accessToken = (string)json["access_token"];
                        Credentials.tokenExpiry = DateTime.UtcNow.AddSeconds((double)json["expires_in"]);
                        Credentials.Save();

                        if (Credentials.accessToken == null)
                        {
                            throw new EliteDangerousCompanionAppAuthenticationException("Access token not found");
                        }

                        CurrentState = State.Authorized;
                    }
                    else
                    {
                        throw new EliteDangerousCompanionAppAuthenticationException("Invalid refresh token from authorization server");
                    }
                }

            }
            catch (Exception)
            {
                CurrentState = State.LoggedOut;
            }
        }

        private Dictionary<string, string> ParseQueryString(string url)
        {
            // Sadly System.Web.HttpUtility.ParseQueryString() is not available to us
            // https://stackoverflow.com/questions/659887/get-url-parameters-from-a-string-in-net
            Uri myUri = new Uri(url);
            string query = myUri.Query.TrimStart('?');
            string[] queryParams = query.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            var paramValuePairs = queryParams.Select(parameter => parameter.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries));
            var sanitizedValuePairs = paramValuePairs.GroupBy(
                parts => parts[0],
                parts => parts.Length > 2 ? string.Join("=", parts, 1, parts.Length - 1) : (parts.Length > 1 ? parts[1] : ""));
            Dictionary<string, string> paramsDict = sanitizedValuePairs.ToDictionary(
                grouping => grouping.Key,
                grouping => string.Join(",", grouping));
            return paramsDict;
        }

        // all of the three endpoints may:
        //   may return null if frontier is not available, or the data failed to be retreived
        //   may cause a logout/askforlogin because of the above

        // obtain profile end point - we cache it for 30 seconds to reduce requests.  May return null if not available

        public string Profile(bool forceRefresh = false)
        {
            if (!forceRefresh && cachedProfile != null && cachedProfileExpires > DateTime.UtcNow)
            {
                System.Diagnostics.Debug.WriteLine("Returning cached profile");
            }
            else
            {
                string v = Get(PROFILE_URL);
                cachedProfile = v;

                if (cachedProfile != null)
                {
                    cachedProfileExpires = DateTime.UtcNow.AddSeconds(30);
                }
            }

            return cachedProfile;
        }

        // obtain market end point - may return null

        public string Market()
        {
            return Get(MARKET_URL);
        }

        // obtain shipyard end point - may return null

        public string Shipyard()
        {
            return Get(SHIPYARD_URL);
        }

        // will try and get endpoint. Null means a web problem or we are not authorised.

        private string Get(string endpoint)
        {
            if (CurrentState != State.Authorized)
                return null;

            string serverurl = GameIsBeta ? BETA_SERVER : LIVE_SERVER;

            if (Credentials.Expired )
            {
                try
                {
                    RefreshToken();     
                }
                catch (EliteDangerousCompanionWebException ws)
                {
                    System.Diagnostics.Debug.WriteLine(ws);
                    return null;
                }
                catch (Exception ex)        // any other and we are logged out
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                    AskForLogin();          // ask for a login
                    return null;
                }
            }

            // we have access..
            System.Diagnostics.Debug.Assert(CurrentState == State.Authorized);

            HttpWebRequest request = GetRequest(serverurl + endpoint);

            using (HttpWebResponse response = GetResponse(request))
            {
                if (response == null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to contact API server");
                    return null;
                }

                if (response.StatusCode == HttpStatusCode.Found)
                {
                    return null;
                }

                return getResponseData(response);
            }
        }


        /**
         * Obtain the response data from an HTTP web response
         */
        private string getResponseData(HttpWebResponse response)
        {
            if (response is null) { return null; }
            // Obtain and parse our response
            var encoding = response.CharacterSet == ""
                    ? Encoding.UTF8
                    : Encoding.GetEncoding(response.CharacterSet ?? string.Empty);

            System.Diagnostics.Debug.WriteLine("Reading response");
            using (var stream = response.GetResponseStream())
            {
                if (stream == null)
                {
                    System.Diagnostics.Debug.WriteLine("No response stream");
                    return null;
                }
                var reader = new StreamReader(stream, encoding);
                string data = reader.ReadToEnd();
                if (string.IsNullOrEmpty(data) || data.Trim() == "")
                {
                    System.Diagnostics.Debug.WriteLine("No data returned");
                    return null;
                }
                System.Diagnostics.Debug.WriteLine("Data is " + data);
                return data;
            }
        }

        // Set up a request with the correct parameters for talking to the companion app
        private HttpWebRequest GetRequest(string url)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.AllowAutoRedirect = true;
            request.Timeout = 10000;
            request.ReadWriteTimeout = 10000;
            request.UserAgent = useragent;
            if (CurrentState == State.Authorized)
            {
                request.Headers["Authorization"] = $"Bearer {Credentials.accessToken}";
            }

            return request;
        }

        // Obtain a response, ensuring that we obtain the response's cookies
        private HttpWebResponse GetResponse(HttpWebRequest request)
        {
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException wex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to obtain response, error code " + wex.Status);
                return null;
            }
            System.Diagnostics.Debug.WriteLine("Response is " );
            return response;
        }

        private string base64UrlEncode(byte[] blob)
        {
            string base64 = Convert.ToBase64String(blob, Base64FormattingOptions.None);
            return base64.Replace('+', '-').Replace('/', '_').Replace("=", "");
        }


        public static string SafeFileString(string normal)
        {
            normal = normal.Replace("*", "_star");      // common ones rename
            normal = normal.Replace("/", "_slash");
            normal = normal.Replace("\\", "_slash");
            normal = normal.Replace(":", "_colon");
            normal = normal.Replace("?", "_qmark");

            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            foreach (char c in invalid)
                normal = normal.Replace(c, '_'); // all others _

            return normal;
        }

        // Implementation instructions from Frontier: https://hosting.zaonce.net/docs/oauth2/instructions.html
        private static readonly string LIVE_SERVER = "https://companion.orerve.net";
        private static readonly string BETA_SERVER = "https://pts-companion.orerve.net";
        private static readonly string AUTH_SERVER = "https://auth.frontierstore.net";

        private static readonly string SCOPE = "scope=capi";
        private static readonly string AUDIENCE = "audience=steam,frontier"; 
        private static readonly string AUTH_URL = "/auth";      
        private static readonly string TOKEN_URL = "/token";    

        private static readonly string PROFILE_URL = "/profile";
        private static readonly string MARKET_URL = "/market";
        private static readonly string SHIPYARD_URL = "/shipyard";

        private readonly string clientID; // we are not allowed to check the client ID into version control or publish it to 3rd parties

        private string uri;
        private string useragent;

        private string cachedProfile;
        private DateTime cachedProfileExpires;

        private string verifier;
        private string authSessionID;

        private string credentialpath;


    }

    public class EliteDangerousCompanionAppAuthenticationException : Exception
    {
        public EliteDangerousCompanionAppAuthenticationException(string message) : base(message) { }
    }

    public class EliteDangerousCompanionWebException : Exception
    {
        public EliteDangerousCompanionWebException(string message) : base(message) { }
    }

    
}
