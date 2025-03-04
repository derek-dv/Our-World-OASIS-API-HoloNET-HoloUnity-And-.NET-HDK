﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using static NextGenSoftware.OASIS.API.Core.Managers.AvatarManager;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    // This interface is responsbile for persisting data/state to blockchain storage providers.
    public interface IOASISBlockchainStorageProvider : IOASISStorageProvider
    {
        //Blockchain providers have version control built in because they always store a new record rather than updating an existing.
        //Central Storage/DB's by default can update the same record, if this flag is set below then they will act more like a Blockchain and store a new copy of the record and link to the previous version.
        //public bool IsVersionControlEnabled { get; set; } //You cannot turn VersionControl off for Blockchains because it is built in.


        public OASISResult<bool> SendTrasaction(IWalletTransaction transation);
        public Task<OASISResult<bool>> SendTrasactionAsync(IWalletTransaction transation);
        //public OASISResult<bool> SendNFT(IWalletTransaction transation);
        //public Task<OASISResult<bool>> SendNFTAsync(IWalletTransaction transation);

        //TODO: More specefic Blockchain options will appear here soon... ;-)
    }
}