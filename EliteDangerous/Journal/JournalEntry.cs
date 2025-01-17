﻿/*
 * Copyright © 2016-2019 EDDiscovery development team
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

using EliteDangerousCore.DB;
using EliteDangerousCore.JournalEvents;
using BaseUtils.JSON;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace EliteDangerousCore
{
    [DebuggerDisplay("Event {EventTypeStr} {EventTimeUTC} JID {Id} C {CommanderId}")]
    public abstract partial class JournalEntry
    {
        #region Public Instance properties and fields

        public long Id { get; private set; }                    // this is the entry ID
        public long TLUId { get; private set; }                 // this ID of the journal tlu (aka TravelLogId)
        public int CommanderId { get; private set; }            // commander Id of entry

        public JournalTypeEnum EventTypeID { get; private set; }
        public string EventTypeStr { get { return EventTypeID.ToString(); } }             // name of event. these two duplicate each other, string if for debuggin in the db view of a browser

        public System.Drawing.Image Icon { get { return JournalTypeIcons.ContainsKey(this.IconEventType) ? JournalTypeIcons[this.IconEventType] : JournalTypeIcons[JournalTypeEnum.Unknown]; } }   // Icon to paint for this
        public string GetIconPackPath { get { return "Journal." + IconEventType.ToString(); } } // its icon pack name..

        public DateTime EventTimeUTC { get; set; }

        public DateTime EventTimeLocal { get { return EventTimeUTC.ToLocalTime(); } }

        public bool SyncedEDSM { get { return (Synced & (int)SyncFlags.EDSM) != 0; } }
        public bool SyncedEDDN { get { return (Synced & (int)SyncFlags.EDDN) != 0; } }
        public bool StartMarker { get { return (Synced & (int)SyncFlags.StartMarker) != 0; } }
        public bool StopMarker { get { return (Synced & (int)SyncFlags.StopMarker) != 0; } }

        public virtual bool IsBeta { get { return TravelLogUnit.Get(TLUId)?.Beta ?? false; } }        // TLUs are cached via the dictionary, no point also holding a local copy
        public virtual bool IsHorizons { get { return TravelLogUnit.Get(TLUId)?.Horizons ?? false; } }     
        public virtual bool IsOdyssey { get { return TravelLogUnit.Get(TLUId)?.Odyssey ?? false; } }       

        public abstract void FillInformation(ISystem sys, out string info, out string detailed);     // all entries must implement

        // the long name of it, such as Approach Body. May be overridden, is translated
        public virtual string SummaryName(ISystem sys) { return TranslatedEventNames.ContainsKey(EventTypeID) ? TranslatedEventNames[EventTypeID] : EventTypeID.ToString(); }  // entry may be overridden for specialist output

        // the name used to filter it.. and the filter keyword. Its normally the enum of the event.
        public virtual string EventFilterName { get { return EventTypeID.ToString(); } } // text name used in filter

        #endregion

        #region Special Setters - db not updated by them

        public void SetTLUCommander(long t, int cmdr)         // used during log reading..
        {
            TLUId = t;
            CommanderId = cmdr;
        }

        public void SetCommander(int cmdr)         // used during log reading..
        {
            CommanderId = cmdr;
        }

        #endregion

        #region Setters - db is updated

        public void SetStartFlag()
        {
            UserDatabase.Instance.ExecuteWithDatabase(cn => UpdateSyncFlagBit(SyncFlags.StartMarker, true, SyncFlags.StopMarker, false, cn.Connection));
        }

        public void SetEndFlag()
        {
            UserDatabase.Instance.ExecuteWithDatabase(cn => UpdateSyncFlagBit(SyncFlags.StartMarker, false, SyncFlags.StopMarker, true, cn.Connection));
        }

        public void ClearStartEndFlag()
        {
            UserDatabase.Instance.ExecuteWithDatabase(cn => UpdateSyncFlagBit(SyncFlags.StartMarker, false, SyncFlags.StopMarker, false, cn.Connection));
        }

        public void SetEdsmSync()
        {
            UserDatabase.Instance.ExecuteWithDatabase(cn => UpdateSyncFlagBit(SyncFlags.EDSM, true, SyncFlags.NoBit, false, cn.Connection));
        }

        internal void SetEdsmSync(SQLiteConnectionUser cn , DbTransaction txn = null)
        {
            UpdateSyncFlagBit(SyncFlags.EDSM, true, SyncFlags.NoBit, false, cn, txn);
        }

        public void SetEddnSync()
        {
            UserDatabase.Instance.ExecuteWithDatabase( cn => UpdateSyncFlagBit(SyncFlags.EDDN, true, SyncFlags.NoBit, false, cn.Connection));
        }

        public static void SetEdsmSyncList(List<JournalEntry> jlist)
        {
            UserDatabase.Instance.ExecuteWithDatabase(cn =>
            {
                using (var txn = cn.Connection.BeginTransaction())
                {
                    foreach (var he in jlist)
                        he.SetEdsmSync(cn.Connection, txn);
                    txn.Commit();
                }
            });
        }


        #endregion

        #region Event Information - return event enums/icons/text etc.

        // return JEnums with events matching optional methods, unsorted
        static public List<JournalTypeEnum> GetEnumOfEvents(string[] methods = null)
        {
            List<JournalTypeEnum> ret = new List<JournalTypeEnum>();

            foreach (JournalTypeEnum jte in Enum.GetValues(typeof(JournalTypeEnum)))
            {
                if ((int)jte < (int)JournalTypeEnum.ICONIDsStart)
                {
                    if (methods == null)
                    {
                        ret.Add(jte);
                    }
                    else
                    {
                        Type jtype = TypeOfJournalEntry(jte);

                        if (jtype != null)      // may be null, Unknown for instance
                        {
                            foreach (var n in methods)
                            {
                                if (jtype.GetMethod(n) != null)
                                {
                                    ret.Add(jte);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return ret;
        }

        // return name instead of enum, unsorted
        static public List<string> GetNameOfEvents(string[] methods = null)
        {
            var list = GetEnumOfEvents(methods);
            return list.Select(x => x.ToString()).ToList();
        }

        // enum name, translated name, image
        static public List<Tuple<string, string, Image>> GetNameImageOfEvents(string[] methods = null, bool sort = false)
        {
            List<JournalTypeEnum> jevents = JournalEntry.GetEnumOfEvents(methods);

            var list = jevents.Select(x => new Tuple<string, string, Image>(x.ToString(), TranslatedEventNames[x],
                JournalTypeIcons.ContainsKey(x) ? JournalTypeIcons[x] : JournalTypeIcons[JournalTypeEnum.Unknown])).ToList();

            if (sort)
            {
                list.Sort(delegate (Tuple<string, string, Image> left, Tuple<string, string, Image> right)     // in order, oldest first
                {
                    return left.Item2.ToString().CompareTo(right.Item2.ToString());
                });
            }

            return list;
        }

        static public Tuple<string, string, Image> GetNameImageOfEvent(JournalTypeEnum ev)
        {
            return new Tuple<string, string, Image>(ev.ToString(), TranslatedEventNames[ev],
                JournalTypeIcons.ContainsKey(ev) ? JournalTypeIcons[ev] : JournalTypeIcons[JournalTypeEnum.Unknown]);
        }

        #endregion

        #region Factory creation

        static public JournalEntry CreateJournalEntry(string events, DateTime t)            
        {
            JObject jo = new JObject();
            jo.Add("event", events);
            jo.Add("timestamp", t);
            return CreateJournalEntry(jo.ToString());
        }

        // Decode text, to journal entry, or Unknown/Null if bad
        static public JournalEntry CreateJournalEntry(string text, bool savejson = false, bool returnnullifbadjson = false)       
        {
            JObject jo = JToken.Parse(text, JToken.ParseOptions.AllowTrailingCommas | JToken.ParseOptions.CheckEOL).Object();
            JournalEntry ret = null;

            if (jo != null)         // good json
            {
                string eventname = jo["event"].StrNull();

                if (eventname != null)  // has an event name, therefore worth keeping
                {
                    if (ClassActivators.TryGetValue(eventname, out var act))        // if known, make it
                        ret = act(jo);
                    else
                    {
                        ret = new JournalUnknown(jo);           // else make a unknown one
                        System.Diagnostics.Debug.WriteLine("Not Recognised event " + jo.ToString());
                    }
                }
            }

            if ( ret == null )                      // no journal line
            {
                if (returnnullifbadjson)            // if we just want to dump it, return null
                    return null;

                jo = new JObject();                 // otherwise, make a JSON for display purposes with BadJSON with the text in
                jo["BadJSON"] = text;               // used if we read bad JSON from the DB
                ret = new JournalUnknown(jo);       // unknown
                savejson = true;                    // need to keep this JSON as we made this up
                System.Diagnostics.Debug.WriteLine("Bad JSON" + text);
            }

            if (savejson)
                ret.JsonCached = jo;

            return ret;
        }

        #endregion

        #region Types of events

        static public Type TypeOfJournalEntry(string text)
        {
            Type t = Type.GetType(JournalRootClassname + ".Journal" + text, false, true); // no exception, ignore case here
            return t;
        }

        static public Type TypeOfJournalEntry(JournalTypeEnum type)
        {
            if (JournalEntryTypes.ContainsKey(type))
            {
                return JournalEntryTypes[type];
            }
            else
            {
                return TypeOfJournalEntry(type.ToString());
            }
        }

        #endregion


        #region Private variables

        private enum SyncFlags
        {
            NoBit = 0,                      // for sync change func only
            EDSM = 0x01,
            EDDN = 0x02,
            // 0x04 was EGO
            StartMarker = 0x0100,           // measure distance start pos marker
            StopMarker = 0x0200,            // measure distance stop pos marker
        }
        private int Synced { get; set; }                     // sync flags

        #endregion

        #region Virtual overrides

        protected virtual JournalTypeEnum IconEventType { get { return EventTypeID; } }  // entry may be overridden to dynamically change icon event for an event

        #endregion

        #region Constructors

        protected JournalEntry(DateTime utc, JournalTypeEnum jtype, bool edsmsynced)       // manual creation via NEW
        {
            EventTypeID = jtype;
            EventTimeUTC = utc;
            Synced = edsmsynced ? (int)SyncFlags.EDSM : 0;
            TLUId = 0;
        }

        protected JournalEntry(JObject jo, JournalTypeEnum jtype)              // called by journal entries to create themselves
        {
            EventTypeID = jtype;
            if (DateTime.TryParse(jo["timestamp"].Str(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime etime))
                EventTimeUTC = etime;
            else
                EventTimeUTC = DateTime.MinValue;
            TLUId = 0;
        }

        #endregion

        #region Private Type info

        private static string JournalRootClassname = typeof(JournalEvents.JournalTouchdown).Namespace;        // pick one at random to find out root classname
        private static Dictionary<JournalTypeEnum, Type> JournalEntryTypes = GetJournalEntryTypes();        // enum -> type

        // Gets the mapping of journal type value to JournalEntry type
        private static Dictionary<JournalTypeEnum, Type> GetJournalEntryTypes()
        {
            Dictionary<JournalTypeEnum, Type> typedict = new Dictionary<JournalTypeEnum, Type>();
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            var types = asm.GetTypes().Where(t => typeof(JournalEntry).IsAssignableFrom(t) && !t.IsAbstract).ToList();

            foreach (Type type in types)
            {
                JournalEntryTypeAttribute typeattrib = type.GetCustomAttributes(false).OfType<JournalEntryTypeAttribute>().FirstOrDefault();
                if (typeattrib != null)
                {
                    typedict[typeattrib.EntryType] = type;
                }
            }

            return typedict;
        }

        //Activators are delegates which can make a specific JournalEntry type.  Deep c# stuff here

        private static Dictionary<string, BaseUtils.ObjectActivator.Activator<JournalEntry>> ClassActivators = GetClassActivators();

        private static Dictionary<string, BaseUtils.ObjectActivator.Activator<JournalEntry>> GetClassActivators()
        {
            var actlist = new Dictionary<string, BaseUtils.ObjectActivator.Activator<JournalEntry>> ();

            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            var types = asm.GetTypes().Where(t => typeof(JournalEntry).IsAssignableFrom(t) && !t.IsAbstract).ToList();

            foreach (Type type in types)
            {
                JournalEntryTypeAttribute typeattrib = type.GetCustomAttributes(false).OfType<JournalEntryTypeAttribute>().FirstOrDefault();
                if (typeattrib != null)
                {
                    System.Reflection.ConstructorInfo ctor = type.GetConstructors().First();        // this is freaking deep c# here.
                    var r = ctor.GetParameters();
                    System.Diagnostics.Debug.Assert(r.Count() == 1);
                    var r0t = r[0].GetType();
                    System.Diagnostics.Debug.Assert(r[0].ParameterType.Name == "JObject");      // checking we are picking the correct ctor
                    actlist[typeattrib.EntryType.ToString()] = BaseUtils.ObjectActivator.GetActivator<JournalEntry>(ctor);
                 //   System.Diagnostics.Debug.WriteLine("Activator " + typeattrib.EntryType.ToString());
                }
            }

            return actlist;
        }


        #endregion

        #region Icons and names

        // enum -> icons 
        public static IReadOnlyDictionary<JournalTypeEnum, Image> JournalTypeIcons { get; } = new BaseUtils.Icons.IconGroup<JournalTypeEnum>("Journal");

        // enum -> Translated Name Events
        private static Dictionary<JournalTypeEnum, string> TranslatedEventNames = GetJournalTranslatedNames();     // precompute the names due to the expense of splitcapsword

        private static Dictionary<JournalTypeEnum, string> GetJournalTranslatedNames()
        {
            var v = Enum.GetValues(typeof(JournalTypeEnum)).OfType<JournalTypeEnum>();
            return v.ToDictionary(e => e, e => e.ToString().SplitCapsWord().Tx(typeof(JournalTypeEnum), e.ToString()));
        }

        #endregion

        #region Helpers

        public static JObject RemoveEDDGeneratedKeys(JObject obj)      // obj not changed
        {
            JObject jcopy = null;

            foreach (var kvp in obj)
            {
                if (kvp.Key.StartsWith("EDD") || kvp.Key.Equals("StarPosFromEDSM"))
                {
                    if (jcopy == null)      // only pay the expense if it has one of the entries in it
                        jcopy = (JObject)obj.Clone();

                    jcopy.Remove(kvp.Key);
                }
            }

            return jcopy != null ? jcopy : obj;
        }

        // optionally pass in json for speed reasons.  Guaranteed that ent1jo and 2 are not altered by the compare!
        internal static bool AreSameEntry(JournalEntry ent1, JournalEntry ent2, SQLiteConnectionUser cn, JObject ent1jo = null, JObject ent2jo = null)
        {
            if (ent1jo == null && ent1 != null)
            {
                ent1jo = GetJson(ent1.Id,cn);      // read from db the json since we don't have it
            }

            if (ent2jo == null && ent2 != null)
            {
                ent2jo = GetJson(ent2.Id,cn);      // read from db the json since we don't have it
            }

            if (ent1jo == null || ent2jo == null)
            {
                return false;
            }

            //System.Diagnostics.Debug.WriteLine("Compare " + ent1jo.ToString() + " with " + ent2jo.ToString());

            // Fixed problem #1518, Prev. the remove was only done on GetJson's above.  
            // during a scan though, ent1jo is filled in, so the remove was not being performed on ent1jo.
            // So if your current map colour was different in FSD entries then
            // the newly created entry would differ from the db version by map colour - causing #1518
            // secondly, this function should not alter ent1jo/ent2jo as its a compare function.  it was.  Change RemoveEDDGenKeys to copy if it alters it.

            JObject ent1jorm = RemoveEDDGeneratedKeys(ent1jo);     // remove keys, but don't alter originals as they can be used later 
            JObject ent2jorm = RemoveEDDGeneratedKeys(ent2jo);

            bool res = JToken.DeepEquals(ent1jorm, ent2jorm);
            //if (!res) System.Diagnostics.Debug.WriteLine("!! Not duplicate {0} vs {1}", ent1jorm.ToString(), ent2jorm.ToString()); else  System.Diagnostics.Debug.WriteLine("Duplicate");
            return res;
        }

        protected JObject ReadAdditionalFile( string extrafile, bool waitforfile, bool checktimestamptype )       // read file, return new JSON
        {
            for (int retries = 0; retries < 25*4 ; retries++)
            {
                // this has the full version of the event, including data, at the same timestamp
                string json = BaseUtils.FileHelpers.TryReadAllTextFromFile(extrafile);      // null if not there, or locked..

                // decode into JObject if there, null if in error or not there
                JObject joaf = json != null ? JObject.Parse(json, JToken.ParseOptions.AllowTrailingCommas | JToken.ParseOptions.CheckEOL) : null;

                if (joaf != null)
                {
                    string newtype = joaf["event"].Str();
                    DateTime fileUTC = joaf["timestamp"].DateTimeUTC();
                    if (newtype != EventTypeStr || fileUTC == DateTime.MinValue)
                    {
                        System.Diagnostics.Debug.WriteLine($"Rejected {extrafile} due to type/bad date, deleting");
                        BaseUtils.FileHelpers.DeleteFileNoError(extrafile);     // may be corrupt..
                        return null;
                    }
                    else
                    {
                        if (fileUTC > EventTimeUTC)
                        {
                          //  System.Diagnostics.Debug.WriteLine($"File is younger than Event, can't be associated {extrafile}");
                            return null;
                        }
                        else if (checktimestamptype == false || fileUTC == EventTimeUTC)
                        {
                            System.Diagnostics.Debug.WriteLine($"Read {extrafile} at {fileUTC} after {retries}");
                            return joaf;                        // good current file..
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"File not written yet due to timestamp {extrafile} at {fileUTC}, waiting.. {retries}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Cannot read {extrafile}, waiting.. {retries}");
                }

                if (!waitforfile)               // if don't wait, continue with no return
                    return null;

                System.Threading.Thread.Sleep(25);
            }

            return null;
        }

        #endregion

    }
}

