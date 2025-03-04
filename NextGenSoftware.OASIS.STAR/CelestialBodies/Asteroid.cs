﻿using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Asteroid : CelestialBody<Asteroid>, IAsteroid
    {
        public Asteroid() : base(HolonType.Asteroid){}

        public Asteroid(Guid id) : base(id, HolonType.Asteroid) {}

        public Asteroid(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Asteroid) {} 
    }
}
