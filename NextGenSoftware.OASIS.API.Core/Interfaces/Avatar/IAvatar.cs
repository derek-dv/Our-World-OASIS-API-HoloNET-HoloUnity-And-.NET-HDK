﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IAvatar : IHolonBase
    {
        Dictionary<ProviderType, List<IProviderWallet>> ProviderWallets { get; set; }
       // Dictionary<ProviderType, string> ProviderPrivateKey { get; set; } //TODO: Want to replace this with ProviderWallets above ASAP...
       // Dictionary<ProviderType, List<string>> ProviderPublicKey { get; set; } //TODO: Want to replace this with ProviderWallets above ASAP...
        Dictionary<ProviderType, string> ProviderUsername { get; set; }
       // Dictionary<ProviderType, List<string>> ProviderWalletAddress { get; set; } //TODO: Want to replace this with ProviderWallets above ASAP...
        Guid AvatarId { get; set; }
        string Title { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string FullName { get; }
        string Username { get; set; }
        string Email { get; set; }
        string Password { get; set; }
        EnumValue<AvatarType> AvatarType { get; set; }
        bool AcceptTerms { get; set; }
        bool IsVerified { get; }
        string JwtToken { get; set; }
        DateTime? PasswordReset { get; set; }
        string RefreshToken { get; set; }
        List<RefreshToken> RefreshTokens { get; set; }
        string ResetToken { get; set; }
        DateTime? ResetTokenExpires { get; set; }
        string VerificationToken { get; set; }
        DateTime? Verified { get; set; }
        DateTime? LastBeamedIn { get; set; }
        DateTime? LastBeamedOut { get; set; }
        bool IsBeamedIn { get; set; }
        //string Image2D { get; set; }
        //int Karma { get; set; }
        //int Level { get; }
        //int XP { get; set; }
        bool OwnsToken(string token);
        OASISResult<IAvatar> Save(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IAvatar>> SaveAsync(ProviderType providerType = ProviderType.Default);
        //OASISResult<bool> SaveProviderWallets(ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<bool>> SaveProviderWalletsAsync(ProviderType providerType = ProviderType.Default);
    }
}