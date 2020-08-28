﻿using System;
using System.Collections.Generic;
using System.Text;

namespace NextGenSoftware.OASIS.API.Core
{
    public interface IHolon
    {
        Guid Id { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        string ProviderKey { get; set; }
        HolonType HolonType { get; set; }
        IPlanet Planet { get; set; }
        IMoon Moon { get; set; }
        IHolon Parent { get; set; }
        List<IHolon> Children { get; set; }
    }
}
