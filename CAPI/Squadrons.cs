/*
 * Copyright © 2025-2025 Robby & EDDiscovery development team
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

using BaseUtils;
using QuickJSON;
using System;
using System.Collections.Generic;

namespace CAPI
{
    // this decodes the JSON squadron from Frontiers CAPI for you

    public class Squadrons : CAPIEndPointBaseClass
    {
        public Squadrons(string jsonstr) : base(jsonstr)
        {
            Carrier = new FleetCarrier(json["squadronCarrier"].ToString());
            JArray mem = json["members"].Array();
            if ( mem != null )
            {
                Members = new Member[mem.Count];
                for (int i = 0; i < mem.Count; i++)
                    Members[i] = new Member(mem[i].Object());
            }

            var commodities = json["bank"].I("commodities").Object();
            if (commodities != null)
            {
                Commodities = new Dictionary<string, List<Commodity>>();
                foreach (var kvp in commodities)
                {
                    var clist = GetCommodityList(kvp.Value.Array());
                    Commodities[kvp.Key] = clist;
                }
            }

            var microresources = json["bank"].I("microresources").Object();
            if (microresources != null)
            {
                MicroResources = new Dictionary<string, List<Commodity>>();
                foreach (var kvp in microresources)
                {
                    var clist = GetCommodityList(kvp.Value.Array());
                    MicroResources[kvp.Key] = clist;
                }
            }
        }

        public bool IsValid { get { return json != null && ID != long.MinValue && Name != null; } }

        // Commander

        public long ID { get { return json["id"].Long(long.MinValue); } }
        public string Name { get { return json["name"].StrNull(); } }
        public DateTime Created { get { return json["name"].DateTimeUTC(); } }
        public bool AcceptingNewMembers { get { return json["acceptingNewMembers"].Bool(); } }
        public string PowerName { get { return json["superpowerName"].StrNull(); } }
        public string FactionName { get { return json["factionName"].StrNull(); } }
        public string FactionHomeSystem { get { return json["factionHomeSystemName"].StrNull(); } }
        public long FactionHomeSystemAddress { get { return json["factionHomeSystemId"].Long(); } }
        public int MemberCount { get { return json["memberCount"].Int(); } }
        public string MissionStatement { get { return json["description"].StrNull(); } }
        public string Motto { get { return json["motto"].StrNull(); } }
        public int Active7Days { get { return json["active7Days"].Int(); } }
        public int Active30Days { get { return json["active30Days"].Int(); } }
        public string PrimaryPerk { get { return json["perks"].I("primary").Str(); } }
        public string SecondaryPerk { get { return json["perks"].I("secondary").Str(); } }
        public FleetCarrier Carrier { get; private set; }
        public long Credits { get { return json["bank"].I("credits").I("All").I(0).I("qty").Str("0")?.InvariantParseLong(0) ?? 0; } }
        public long CarrierCredits { get { return json["bank"].I("credits").I("Carrier Balance").I(0).I("qty").Str("0")?.InvariantParseLong(0) ?? 0; } }
        public Dictionary<string, List<Commodity>> Commodities { get; private set; }        // may be null
        public Dictionary<string, List<Commodity>> MicroResources { get; private set; }     // may be null

        public class Member
        {
            public Member(JObject data)
            {
                json = data;
            }

            public JObject json { get; }

            public long ID => json["member_id"].Long();
            public string Name => json["name"].Str().FromHexString();
            public DateTime Joined { get { return json["joined"].DateTimeUTC(); } }
            public DateTime LastOnline { get { return json["lastOnline"].DateTimeUTC(); } }
            public string Status { get { return json["status"].StrNull(); } }
            public string ShipName { get { return json["shipName"].StrNull(); } }
            public string ShipModel { get { return json["shipModel"].StrNull(); } }
            public int Rank => json["rank_id"].Int();
            public int RankCombat => json["rankCombat"].Int();
            public int RankExplore => json["rankExplore"].Int();
            public int RankTrade => json["rankTrade"].Int();
            public int RankCQC => json["rankCqc"].Int();
            public int RankSoldier => json["soldierRank"].Int();
            public int RankExobiologist => json["exobiologistRank"].Int();
            public int RankEmpire => json["empireRank"].Int();
            public int RankFederation => json["federationRank"].Int();
            public int RankPower => json["powerRank"].Int();
            public int UserPlayTime => json["userPlayTime"].Int();
            public string LastLocation => json["lastLocation"].Str().FromHexString();
            public string RequestLetter => json["requestletter"].Str().FromHexString();
            public string Biography => json["biography"].Str().FromHexString();
        }

        public Member[] Members { get; private set; }

        //.. "bank"

    }
}
