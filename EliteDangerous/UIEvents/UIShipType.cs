﻿/*
 * Copyright © 2016 - 2021 EDDiscovery development team
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

namespace EliteDangerousCore.UIEvents
{
    public class UIShipType : UIEvent
    {
        public UIShipType(DateTime time, bool refresh) : base(UITypeEnum.ShipType, time, refresh)
        {
        }

        public UIShipType(Shiptype type, DateTime time, bool refresh) : this(time, refresh)
        {
            ShipType = type;
        }

        public enum Shiptype {
            None,
            MainShipNormalSpace,
            MainShipDockedStarPort,
            MainShipDockedPlanet,
            MainShipSupercruise,
            MainShipLanded,
            TaxiDocked,
            TaxiNormalSpace,
            TaxiSupercruise,
            TaxiDockedPlanet,
            MulticrewDockedStarPort,
            MulticrewDockedPlanet,
            MulticrewNormalSpace,
            MulticrewSupercruise,
            MulticrewLanded,
            MulticrewSRV,
            SRV,
            Fighter,
            OnFootStarPortHangar,
            OnFootStarPortSocialSpace,
            OnFootPlantaryPortHangar,
            OnFootPlantaryPortSocialSpace,
            OnFootInstallationInside,
            OnFootPlanet
        };

        public Shiptype ShipType { get; private set; }

        static public bool OnFoot(Shiptype t ) { return t >= Shiptype.OnFootStarPortHangar; }
    }
}
