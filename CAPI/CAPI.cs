/*
 * Original Code from EDDI https://github.com/EDCD/EDDI Thanks for the EDDI team for this
 * 
 * Modified code Copyright © 2022 Robby & EDDiscovery development team
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
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using QuickJSON;

namespace CAPI
{
    public partial class CompanionAPI
    {
        public enum State
        {
            LoggedOut,
            AwaitingCallback,
            Authorized,
            // not states, but messages in StatusChange
            AuthorizationFailed,
            RefreshSucceeded,
        };

        public Action<State> StatusChange { get; set; }        

        public State CurrentState { get; set; } = State.LoggedOut;
        public CompanionAppCredentials Credentials;
        public bool Active => CurrentState == State.Authorized;
        public bool LoggedOut => CurrentState == State.LoggedOut;
        public bool ClientIDAvailable { get { return !string.IsNullOrEmpty(clientID); } }

        public string User { get; private set; }                        // current user
        public string URI { get; private set; }                         // URI for callback
        public string UserAgentNameVersion { get; private set; }        // GreatProgram-1.2.3.4

        public enum CAPIServerType
        {
            Live,
            Legacy,
            Beta,
        }

        public CAPIServerType CAPIServer { get; set; } = CAPIServerType.Live;    
        public string CAPIURI { get { return CAPIServer == CAPIServerType.Live ? LIVE_SERVER : CAPIServer == CAPIServerType.Legacy ? LEGACY_SERVER : BETA_SERVER; } }
            
        public CompanionAPI(string credentialpath, string clientinfo, string useragent, string uri)
        {
            this.credentialpath = credentialpath;
            this.clientID = clientinfo;
            this.URI = uri;
            this.UserAgentNameVersion = useragent;
        }

        // is the user credential file present, and if so, has it tokens so its been logged in

        public enum UserState { NeverLoggedIn, HasLoggedIn, HasLoggedInWithCredentials };

        public UserState GetUserState(string username)
        {
            string credfile = Path.Combine(credentialpath, SafeFileString(username) + ".cred");
            System.Diagnostics.Debug.WriteLine($"CAPI - User state of commander {username} at {credfile}");
            if (File.Exists(credfile))
            {
                var credentials = CompanionAppCredentials.Load(credfile);
                return credentials.IsAccessRefreshTokenPresent ? UserState.HasLoggedInWithCredentials : UserState.HasLoggedIn;
            }
            else
                return UserState.NeverLoggedIn;
        }

        // log out for specific user, true if user existed and logged out. Removes credential file

        public bool LogOut(string username)
        {
            string credfile = Path.Combine(credentialpath, SafeFileString(username) + ".cred");
            System.Diagnostics.Debug.WriteLine($"CAPI - Logout of commander {username} at {credfile}");

            if (File.Exists(credfile))
            {
                try
                {
                    if ( User == username)      // if we are doing it to ourselves, logout
                        LogOut();

                    File.Delete(credfile);
                    return true;
                }
                catch( Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("CAPI Logout exception" + ex);
                }
            }
            return false;
        }

        // login for user, we can login over another user
        // returns true if operating okay (may be performing user login) or false if can't log in

        public bool LogIn(string username)           
        {
            System.Diagnostics.Debug.WriteLine($"CAPI - Login of commander {username}");
            if (!ClientIDAvailable)                                 // must have an ID, else service is disabled
                return false;

            if (username == User && CurrentState != State.LoggedOut)     // if logged in to the same user
                return true;

            string credfile = Path.Combine(credentialpath, SafeFileString(username) + ".cred");
            Credentials = CompanionAppCredentials.Load(credfile);

            User = username;

            CurrentState = State.LoggedOut;         // clear state back to logged out
            cachedProfile = null;                   // empty any cached profile

            try
            {
                RefreshToken();
            }
            catch (EliteDangerousCompanionWebException ws)                 // web exceptions, they happen. don't lost the refresh token over it
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

        // Disconnect the user from this class - credential file is unaffected.  Can call before login
        public void Disconnect()
        {
            bool notloggedout = CurrentState != State.LoggedOut;

            Credentials?.Clear();
            CurrentState = State.LoggedOut;
            cachedProfile = null;
            User = null;

            if ( notloggedout )       // prevent reporting if currently logged out
                StatusChange?.Invoke(CurrentState);
        }

        // Log out of the companion API and clear local credentials
        public void LogOut()
        {
            Disconnect();
            Credentials?.Save();     // if we had a credential file, then clear them in the file
        }

        // throws web exception, or AuthenticationException (logs you out).  Else its got the access token and your good to go

        private void RefreshToken()  // may throw
        {
            if (Credentials.refreshToken == null)
            {
                LogOut();
                throw new EliteDangerousCompanionAppAuthenticationException("Refresh token not found, need full login");
            }

            HttpWebRequest request = GetRequest(AUTH_SERVER + TOKEN_URL);
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            byte[] data = Encoding.UTF8.GetBytes($"grant_type=refresh_token&client_id={clientID}&refresh_token={Credentials.refreshToken}");
            request.ContentLength = data.Length;
            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(data, 0, data.Length);
            }

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
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
                            LogOut();
                            throw new EliteDangerousCompanionAppAuthenticationException("Access token not found");
                        }

                        CurrentState = State.Authorized;
                        StatusChange?.Invoke(State.RefreshSucceeded);
                    }
                    else
                    {
                        LogOut();
                        throw new EliteDangerousCompanionAppAuthenticationException("Invalid refresh token");
                    }
                }
            }
            catch (WebException wex)
            {
                System.Diagnostics.Debug.WriteLine("CAPI Refresh Web exception " + wex.Status);
                if (wex.Status == WebExceptionStatus.ProtocolError)         // seen when a bad refresh token is sent to frontier
                {
                    LogOut();
                    throw new EliteDangerousCompanionWebException("Protocol Error");
                }
                else
                    throw new EliteDangerousCompanionWebException("Failed to contact API server " + wex);
            }
        }

        private void AskForLogin()      
        {
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
            string CALLBACK_URL = $"{URI}://auth/";
            string webURL = $"{AUTH_SERVER}{AUTH_URL}" + $"?response_type=code&{AUDIENCE}&{SCOPE}&client_id={clientID}&code_challenge={codeChallenge}&code_challenge_method=S256&state={authSessionID}&redirect_uri={Uri.EscapeDataString(CALLBACK_URL)}";
            Process.Start(webURL);

            CurrentState = State.AwaitingCallback;
            StatusChange?.Invoke(CurrentState);
        }

        // DDE server calls this with the URL callback
        // NB any user can send an arbitrary URL from the Windows Run dialog, so it must be treated as untrusted

        public void URLCallBack(string url)
        {
            try
            {
                string CALLBACK_URL = $"{URI}://auth/";

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
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
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
                        StatusChange?.Invoke(CurrentState);
                    }
                    else
                    {
                        throw new EliteDangerousCompanionAppAuthenticationException("Invalid refresh token from authorization server");
                    }
                }
            }
            catch (Exception)
            {
                StatusChange?.Invoke(State.AuthorizationFailed);
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

        // all of the endpoints may:
        //   may return null if frontier is not available, or the data failed to be retreived
        //   may cause a logout/askforlogin because of the above
        // all are thread safe

        // check on Active before calling- thats a single variable which should be thread safe 

        // obtain profile end point - we cache it for 30 seconds to reduce requests.  May return null if not available

        public string Profile(bool forceRefresh = false)
        {
            if (!forceRefresh && cachedProfile != null && cachedProfileExpires > DateTime.UtcNow)
            {
                //System.Diagnostics.Debug.WriteLine("Returning cached profile");
            }
            else
            {
                string v = Get(PROFILE_URL, out HttpStatusCode unused);
                cachedProfile = v;                                                  // single point set, should be thread safe.

                if (cachedProfile != null)
                {
                    cachedProfileExpires = DateTime.UtcNow.AddSeconds(30);
                }
            }

            return cachedProfile;
        }

        // obtain market end point - may return null

        public string Market(bool nocontentreturnemptystring = false)
        {
            return Get(MARKET_URL, out HttpStatusCode unused, nocontentreturnemptystring);
        }

        // obtain shipyard end point - may return null

        public string Shipyard(bool nocontentreturnemptystring = false)
        {
            return Get(SHIPYARD_URL, out HttpStatusCode unused, nocontentreturnemptystring);
        }

        // obtain fleetcarrier end point - may return null

        public string FleetCarrier(bool nocontentreturnemptystring = false)
        {
            return Get(FLEETCARRIER_URL, out HttpStatusCode unused, nocontentreturnemptystring);
        }

        // obtain CG end point - may return null
        
        public string CommunityGoals(bool nocontentreturnemptystring = false)
        {
            return Get(COMMUNITYGOALS_URL, out HttpStatusCode unused, nocontentreturnemptystring);
        }

        // obtain journal on date
        // status = OK + string : got
        // status = PartialContent + string = not all loaded
        // status = NoContent = nothing on that day
        // status = ServiceUnavailable/Unauthoriszed - see below

        public string Journal(DateTime day, out HttpStatusCode status)
        {
            return Journal(day.ToString("yyyy/MM/dd", System.Globalization.CultureInfo.InvariantCulture),out status);
        }

        public string Journal(string date, out HttpStatusCode status)       // date is in yyyy/mm/dd or yyyy-mm-dd format
        {
            var s = Get(JOURNAL_URL + "/" + date.Replace("-","/") , out status);
            if ( s != null)
            {
                // if OK/Jorunal unavailable or empty object, fix output to null, thanks Artie.

                if (status == HttpStatusCode.OK && s.Equals("Journal unavailable", StringComparison.InvariantCultureIgnoreCase))
                {
                    s = null;
                }
                else if (s == "{}")
                {
                    s = null;
                }
            }

            return s;
        }

        // will try and get endpoint. may return null
        // status = ServiceUnavailable could not get a response
        // status = Unauthorized = oAuth not working, refresh token not working, or login required
        // or status code from server

        private string Get(string endpoint, out HttpStatusCode status, bool nocontentreturnemptystring = false)
        {
            lock (Credentials)          // we lock here so two threads can't alter the login/credientials at the same time
            {
                status = HttpStatusCode.Unauthorized;

                if (!Active)
                    return null;

                if (Credentials.Expired)
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

                string serverurl = CAPIURI;
                System.Diagnostics.Debug.WriteLine($"CAPI - request {serverurl} {endpoint}");
                HttpWebRequest request = GetRequest(serverurl + endpoint);

                try
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        status = response.StatusCode;

                        if (response.StatusCode == HttpStatusCode.Found)
                        {
                            return null;
                        }

                        if ( response.StatusCode == HttpStatusCode.NoContent && nocontentreturnemptystring)
                        {
                            return "";
                        }

                        return getResponseData(response);
                    }
                }
                catch (WebException wex)
                {
                    status = HttpStatusCode.ServiceUnavailable;
                    System.Diagnostics.Debug.WriteLine("CAPI Failed to obtain response, error code " + wex.Status);
                    return null;
                }
            }
        }


        /**
         * Obtain the response data from an HTTP web response
         */
        private string getResponseData(HttpWebResponse response)
        {
            if (response is null)
            {
                return null;
            }

            // Obtain and parse our response
            var encoding = string.IsNullOrEmpty(response.CharacterSet) ? Encoding.UTF8 : Encoding.GetEncoding(response.CharacterSet ?? string.Empty);

            using (var stream = response.GetResponseStream())
            {
                if (stream == null)
                {
                    System.Diagnostics.Debug.WriteLine("CAPI No response stream");
                    return null;
                }
                var reader = new StreamReader(stream, encoding);
                string data = reader.ReadToEnd();
                if (string.IsNullOrEmpty(data) || data.Trim() == "")
                {
                    System.Diagnostics.Debug.WriteLine("CAPI No data returned");
                    return null;
                }

                //System.Diagnostics.Debug.WriteLine("CAPI Data is " + data);
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
            request.UserAgent = UserAgentNameVersion;
            if (CurrentState == State.Authorized)
            {
                request.Headers["Authorization"] = $"Bearer {Credentials.accessToken}";
            }

            return request;
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

        // Implementation instructions from Frontier: https://hosting.zaonce.net/docs/oauth2/instructions.html, 
        // with Legacy server from the Update 14 info
        private static readonly string LIVE_SERVER = "https://companion.orerve.net";
        private static readonly string LEGACY_SERVER = "https://legacy-companion.orerve.net/";
        private static readonly string BETA_SERVER = "https://pts-companion.orerve.net";
        private static readonly string AUTH_SERVER = "https://auth.frontierstore.net";

        private static readonly string SCOPE = "scope=capi";
        private static readonly string AUDIENCE = "audience=all"; 
        private static readonly string AUTH_URL = "/auth";      
        private static readonly string TOKEN_URL = "/token";    

        private static readonly string PROFILE_URL = "/profile";
        private static readonly string MARKET_URL = "/market";
        private static readonly string SHIPYARD_URL = "/shipyard";
        private static readonly string JOURNAL_URL = "/journal";
        private static readonly string FLEETCARRIER_URL = "/fleetcarrier";
        private static readonly string COMMUNITYGOALS_URL = "/communitygoals";

        private readonly string clientID; // we are not allowed to check the client ID into version control or publish it to 3rd parties

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
