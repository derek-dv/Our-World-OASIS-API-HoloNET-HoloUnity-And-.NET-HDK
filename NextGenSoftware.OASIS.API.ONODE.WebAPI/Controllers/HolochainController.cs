﻿using Microsoft.AspNetCore.Mvc;
using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/holochain")]
    public class HolochainController : OASISControllerBase
    {
        private KeyManager _keyManager = null;

        public KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

                    if (result.IsError)
                        ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider(). Error details: ", result.Message), true, false, true);

                    _keyManager = new KeyManager(result.Result);
                }

                return _keyManager;
            }
        }

        public HolochainController()
        {

        }

        /// <summary>
        /// Get's the Holochain Agent ID(s) for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetHolochainAgentIdsForAvatar")]
        public OASISResult<List<string>> GetHolochainAgentIdsForAvatar(Guid avatarId)
        {
            return KeyManager.GetProviderPublicKeysForAvatarById(avatarId, ProviderType.HoloOASIS);
        }

        /// <summary>
        /// Get's the Holochain Agent's private key's for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetHolochainAgentPrivateKeysForAvatar")]
        public OASISResult<List<string>> GetHolochainAgentPrivateKeysForAvatar(Guid avatarId)
        {
            return KeyManager.GetProviderPrivateKeysForAvatarById(avatarId, ProviderType.HoloOASIS);
        }

        /// <summary>
        /// Get's the Avatar id for the the given EOS account name.
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarIdForHolochainAgentId")]
        public OASISResult<Guid> GetAvatarIdForHolochainAgentId(string agentId)
        {
            //TODO: Test that returning a GUID works?
            return KeyManager.GetAvatarIdForProviderPublicKey(agentId, ProviderType.HoloOASIS);
        }

        /// <summary>
        /// Get's the Avatar for the the given Holochain agent id.
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarForHolochainAgentId")]
        public OASISResult<IAvatar> GetAvatarForHolochainAgentId(string agentId)
        {
            return KeyManager.GetAvatarForProviderPublicKey(agentId, ProviderType.HoloOASIS);
        }

        /// <summary>
        /// Get's the HoloFuel balance for the given agent.
        /// </summary>
        /// <param name="agentID"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetHoloFuelBalanceForAgentId")]
        public OASISResult<string> GetHoloFuelBalanceForAgentId(string agentID)
        {
            return new();
        }

        /// <summary>
        /// Get's the EOSIO balance for the given avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetHoloFuelBalanceForAvatar")]
        public OASISResult<string> GetHoloFuelBalanceForAvatar(Guid avatarId)
        {
            return new();
        }

        /// <summary>
        /// Link's a given holochain AgentId to the given avatar.
        /// </summary>
        /// <param name="walletId">The id of the wallet (if any).</param>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="holochainAgentId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{holochainAgentId}")]
        public OASISResult<Guid> LinkHolochainAgentIdToAvatar(Guid walletId, Guid avatarId, string holochainAgentId, ProviderType providerToLoadSaveAvatarTo = ProviderType.Default)
        {
            return KeyManager.LinkProviderPublicKeyToAvatarById(walletId, avatarId, ProviderType.HoloOASIS, holochainAgentId, providerToLoadSaveAvatarTo);
            //return Program.AvatarManager.LinkPublicProviderKeyToAvatar(avatarId, ProviderType.HoloOASIS, holochainAgentId);
        }
    }
}
