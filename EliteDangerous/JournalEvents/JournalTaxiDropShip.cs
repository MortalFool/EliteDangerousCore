﻿/*
 * Copyright © 2016-2018 EDDiscovery development team
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
using System.Linq;
using System.Text;

namespace EliteDangerousCore.JournalEvents
{
    [JournalEntryType(JournalTypeEnum.BookDropship)]
    public class JournalBookDropship : JournalEntry, ILedgerJournalEntry
    {
        public JournalBookDropship(JObject evt) : base(evt, JournalTypeEnum.BookDropship)
        {
            DestinationSystem = evt["DestinationSystem"].StrNull();
            DestinationLocation = evt["DestinationLocation"].StrNull();
            Cost = evt["Cost"].Long();
        }

        public string DestinationSystem { get; set; }
        public string DestinationLocation { get; set; }
        public long Cost { get; set; }

        public override void FillInformation(ISystem sys, out string info, out string detailed)
        {
            long? cost = Cost > 0 ? Cost : default(long?);
            info = BaseUtils.FieldBuilder.Build("", DestinationSystem, "<: ", DestinationLocation, "Cost: ; cr;N0".T(EDTx.JournalEntry_Cost), cost);
            detailed = "";
        }

        public void Ledger(Ledger mcl)
        {
            if (Cost>0)
                mcl.AddEvent(Id, EventTimeUTC, EventTypeID, "->" + DestinationSystem + ":" + DestinationLocation, -Cost);
        }
    }

    [JournalEntryType(JournalTypeEnum.CancelDropship)]
    public class JournalCancelDropship : JournalEntry, ILedgerJournalEntry
    {
        public JournalCancelDropship(JObject evt) : base(evt, JournalTypeEnum.CancelDropship)
        {
            Refund = evt["Refund"].Long();
        }

        public long Refund { get; set; }

        public override void FillInformation(ISystem sys, out string info, out string detailed)
        {
            long? refund = Refund > 0 ? Refund : default(long?);
            info = BaseUtils.FieldBuilder.Build("", refund);
            detailed = "";
        }

        public void Ledger(Ledger mcl)
        {
            if (Refund > 0)
                mcl.AddEvent(Id, EventTimeUTC, EventTypeID, "", Refund);
        }
    }

    [JournalEntryType(JournalTypeEnum.DropshipDeploy)]
    public class JournalDropshipDeploy : JournalEntry
    {
        public JournalDropshipDeploy(JObject evt) : base(evt, JournalTypeEnum.DropshipDeploy)
        {
        }

        public override void FillInformation(ISystem sys, out string info, out string detailed)
        {
            info = "";//     info = BaseUtils.FieldBuilder.Build("", FriendlyType, "", Count, "< sell price ; cr;N0".T(EDTx.JournalEntry_sellprice), SellPrice, "Total Cost: ; cr;N0".T(EDTx.JournalEntry_TotalCost), TotalSale, "Profit: ; cr;N0".T(EDTx.JournalEntry_Profit), profit);
            detailed = "";
        }
    }

    [JournalEntryType(JournalTypeEnum.BookTaxi)]
    public class JournalBookTaxi : JournalEntry, ILedgerJournalEntry
    {
        public JournalBookTaxi(JObject evt) : base(evt, JournalTypeEnum.BookTaxi)
        {
            DestinationSystem = evt["DestinationSystem"].StrNull();
            DestinationLocation = evt["DestinationLocation"].StrNull();
            Cost = evt["Cost"].Long();
        }

        public string DestinationSystem { get; set; }
        public string DestinationLocation { get; set; }
        public long Cost { get; set; }

        public override void FillInformation(ISystem sys, out string info, out string detailed)
        {
            long? cost = Cost > 0 ? Cost : default(long?);
            info = BaseUtils.FieldBuilder.Build("", DestinationSystem, "<:", DestinationLocation, "Cost: ; cr;N0".T(EDTx.JournalEntry_Cost), cost);
            detailed = "";
        }

        public void Ledger(Ledger mcl)
        {
            if (Cost > 0)
                mcl.AddEvent(Id, EventTimeUTC, EventTypeID, "->" + DestinationSystem + ":" + DestinationLocation, -Cost);
        }
    }

    [JournalEntryType(JournalTypeEnum.CancelTaxi)]
    public class JournalCancelTaxi : JournalEntry, ILedgerJournalEntry
    {
        public JournalCancelTaxi(JObject evt) : base(evt, JournalTypeEnum.CancelTaxi)
        {
            Refund = evt["Refund"].Long();
        }

        public long Refund { get; set; }

        public override void FillInformation(ISystem sys, out string info, out string detailed)
        {
            long? refund = Refund > 0 ? Refund : default(long?);
            info = BaseUtils.FieldBuilder.Build("", refund);
            detailed = "";
        }

        public void Ledger(Ledger mcl)
        {
            if (Refund > 0)
                mcl.AddEvent(Id, EventTimeUTC, EventTypeID, "", Refund);
        }
    }


}


