﻿using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class BlackHole : CelestialBody<BlackHole>, IBlackHole
    {
        public BlackHole() : base(HolonType.BlackHole) {}

        public BlackHole(Guid id) : base(id, HolonType.BlackHole) { }

        public BlackHole(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.BlackHole) {} 
    }
}