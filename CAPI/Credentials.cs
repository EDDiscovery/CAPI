/*
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

using BaseUtils.JSON;
using System;
using System.IO;

namespace CAPI
{
    public class CompanionAppCredentials
    {
        public string accessToken { get; set; }
        public string refreshToken { get; set; }
        public DateTime tokenExpiry { get; set; } = new DateTime(2000, 1, 1);       // set to a old date, but not min date, because of Expired

        [JsonIgnoreAttribute]
        public bool IsAccessRefreshTokenPresent { get { return accessToken != null && refreshToken != null; } }
        [JsonIgnoreAttribute]
        public bool Expired { get { return DateTime.UtcNow >= tokenExpiry.AddSeconds(-60); } }
        [JsonIgnoreAttribute]
        public string savedPath { get; set; }

        /// <summary>
        /// Clear the information held by credentials.
        /// </summary>
        public void Clear()
        {
            accessToken = null;
            refreshToken = null;
            tokenExpiry = new DateTime(2000, 1, 1);
        }


        public static CompanionAppCredentials Load(string filepath)
        {
            try
            {
                string json = File.ReadAllText(filepath);
                JToken tk = JToken.Parse(json);
                CompanionAppCredentials credentials = JTokenExtensions.ToObject<CompanionAppCredentials>(tk);
                credentials.savedPath = filepath;
                return credentials;
            }
            catch
            {
                return new CompanionAppCredentials() { savedPath = filepath };
            }
        }

        public void Save()
        {
            JObject jo = JToken.FromObject(this).Object();
            File.WriteAllText(savedPath, jo.ToString());
        }

    }
}
