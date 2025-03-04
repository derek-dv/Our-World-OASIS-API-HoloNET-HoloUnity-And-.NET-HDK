﻿using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Ipfs.Http;
using Ipfs.Engine;
using Ipfs;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS
{
    //TODO: Implement OASISResult properly on below methods! :)
    public class IPFSOASIS : OASISStorageProviderBase, IOASISNETProvider
    {
        public IpfsClient IPFSClient;
        public IpfsEngine IPFSEngine; //= new IpfsEngine();

        private List<IAvatarDetail> AvatarsDetailsList;
        private string avatarDetailsFileAddress;
        private Dictionary<string, HolonResume> _idLookup = new Dictionary<string, HolonResume>();
        private OASISDNA _OASISDNA;
        private string _OASISDNAPath;

        public IPFSOASIS()
        {
            OASISDNAManager.LoadDNA();
            _OASISDNA = OASISDNAManager.OASISDNA;
            _OASISDNAPath = OASISDNAManager.OASISDNAPath;

            Init();
        }

        public IPFSOASIS(string OASISDNAPath)
        {
            _OASISDNAPath = OASISDNAPath;
            OASISDNAManager.LoadDNA(_OASISDNAPath);
            _OASISDNA = OASISDNAManager.OASISDNA;
            Init();
        }

        public IPFSOASIS(OASISDNA OASISDNA)
        {
            _OASISDNA = OASISDNA;
            _OASISDNAPath = OASISDNAManager.OASISDNAPath;
            Init();
        }

        public IPFSOASIS(OASISDNA OASISDNA, string OASISDNAPath)
        {
            _OASISDNA = OASISDNA;
            _OASISDNAPath = OASISDNAPath;
            Init();
        }

        private void Init()
        {
            this.ProviderName = "IPFSOASIS";
            this.ProviderDescription = "IPFS Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.IPFSOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }

        public override OASISResult<bool> ActivateProvider()
        {
            IPFSClient = new IpfsClient(_OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString);
            return base.ActivateProvider();
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            IPFSClient.ShutdownAsync();
            IPFSClient = null;
            return base.DeActivateProvider();
        }


        public async Task<string> LoadFileToJson(string address)
        {
            await using var stream = await IPFSClient.FileSystem.ReadFileAsync(address);
            await using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.ToArray();
            return Encoding.ASCII.GetString(ms.ToArray());
        }

        public async Task<string> LoadStringToJson(string address)
        {
            string text = await IPFSClient.FileSystem.ReadAllTextAsync((Cid) address);
            return text;
        }

        /******************************/
        public async Task<Dictionary<string, HolonResume>> LoadLookupToJson()
        {
            try
            {
                string json = await LoadStringToJson(_OASISDNA.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
                _idLookup = JsonConvert.DeserializeObject<Dictionary<string, HolonResume>>(json);
            }
            catch
            {
                _idLookup = new Dictionary<string, HolonResume>();
            }

            return _idLookup;
        }

        public async Task<string> SaveJsonToFile<T>(List<T> list)
        {
            string json = JsonConvert.SerializeObject(list);
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
            return (string) fsn.Id;
        }

        public async Task<string> SaveLookupToFile(Dictionary<string, HolonResume> idLookup)
        {
            string json = JsonConvert.SerializeObject(idLookup);
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);

            _OASISDNA.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress = fsn.Id;
            OASISDNAManager.SaveDNA(_OASISDNAPath, _OASISDNA);

            return fsn.Id;
        }

        public async Task<IAvatar> SaveAvatarToFile(IAvatar avatar)
        {
            //If we have a previous version of this avatar saved, then add a pointer back to the previous version.
            _idLookup = await LoadLookupToJson();
            HolonResume dico = _idLookup.Values.FirstOrDefault(a => a.Id == avatar.Id);

            // in case there is no element in _idlookup dictionary
            if (dico == null)
                dico = new HolonResume();


            if (_idLookup.Count(a => a.Value.Id == avatar.Id) > 0)
                avatar.PreviousVersionProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] =
                    _idLookup.FirstOrDefault(a => a.Value.Id == avatar.Id).Key;

            string json = JsonConvert.SerializeObject(avatar);
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] = fsn.Id;

            // we store just values that we will use as a filter of search in other methods.

            dico.Id = avatar.Id;
            dico.login = avatar.Username;
            dico.password = avatar.Password;
            dico.ProviderUniqueStorageKey = avatar.ProviderUniqueStorageKey;
            dico.email = avatar.Email;
            dico.HolonType = HolonType.Avatar;

            if (_idLookup.Count == 0)
                _idLookup.Add(fsn.Id, dico);
            else
                _idLookup[fsn.Id] = dico;


            await SaveLookupToFile(_idLookup);

            return avatar;
        }

        public async Task<IHolon> SaveHolonToFile(IHolon holon)
        {
            //If we have a previous version of this avatar saved, then add a pointer back to the previous version.
            _idLookup = await LoadLookupToJson();
            HolonResume dico = _idLookup.Values.FirstOrDefault(a => a.Id == holon.Id);

            // in case there is no element in _idlookup dictionary
            if (dico == null)
                dico = new HolonResume();

            if (_idLookup.Count(a => a.Value.Id == holon.Id) > 0)
                holon.PreviousVersionProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] =
                    _idLookup.FirstOrDefault(a => a.Value.Id == holon.Id).Key;

            string json = JsonConvert.SerializeObject(holon);
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] = fsn.Id;

            // we store just values that we will use as a filter of search in other methods.
            dico.Id = holon.Id;
            dico.ProviderUniqueStorageKey = holon.ProviderUniqueStorageKey;
            dico.ParentHolonId = holon.ParentHolonId;
            dico.HolonType = holon.HolonType;

            if (_idLookup.Count == 0)
                _idLookup.Add(fsn.Id, dico);
            else
                _idLookup[fsn.Id] = dico;

            string id = await SaveLookupToFile(_idLookup);
            return holon;
        }

        public async Task<IAvatarDetail> SaveAvatarDetailToFile(IAvatarDetail avatarDetail)
        {
            //If we have a previous version of this avatar saved, then add a pointer back to the previous version.
            _idLookup = await LoadLookupToJson();
            HolonResume dico = _idLookup.Values.FirstOrDefault(a => a.Id == avatarDetail.Id);

            // in case there is no element in _idlookup dictionary
            if (dico == null)
                dico = new HolonResume();


            if (_idLookup.Count(a => a.Value.Id == avatarDetail.Id) > 0)
                avatarDetail.PreviousVersionProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] =
                    _idLookup.FirstOrDefault(a => a.Value.Id == avatarDetail.Id).Key;

            string json = JsonConvert.SerializeObject(avatarDetail);
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
            avatarDetail.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] = fsn.Id;

            // we store just values that we will use as a filter of search in other methods.

            dico.Id = avatarDetail.Id;
            dico.login = avatarDetail.Username;           
            dico.ProviderUniqueStorageKey = avatarDetail.ProviderUniqueStorageKey;
            dico.email = avatarDetail.Email;
            dico.HolonType = HolonType.AvatarDetail;

            if (_idLookup.Count == 0)
                _idLookup.Add(fsn.Id, dico);
            else
                _idLookup[fsn.Id] = dico;


            await SaveLookupToFile(_idLookup);

            return avatarDetail;
        }

        //public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        //{
        //    return await LoadAvatarTemplateAsync(a => a.login == username && a.password == password);
        //}
        /************************************************************/

        public async Task<string> SaveTextToFile(string text)
        {
            var fsn = await IPFSClient.FileSystem.AddTextAsync(text);
            return fsn.Id;
        }


        //public override OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        //{
        //    return LoadAvatarAsync(username, password).Result;
        //}

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            return SaveAvatarAsync(Avatar).Result;
        }

        //public override async Task<IAvatar> SaveAvatarAsync(IAvatar Avatar)
        //{
        //    if (AvatarsList == null)
        //        AvatarsList = new List<IAvatar>();

        //    AvatarsList.Add(Avatar);


        //    avatarFileAddress = await SaveJsonToFile<IAvatar>(AvatarsList);

        //    return Avatar;
        //}

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            return new OASISResult<IAvatar>(await SaveAvatarToFile(avatar));
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchTerm, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<ISearchResults> result = new OASISResult<ISearchResults>();
            OASISResult<IEnumerable<IAvatar>> avatars = await LoadAllAvatarsAsync();
            OASISResult<IEnumerable<IHolon>> holons = await LoadAllHolonsAsync();

            avatars = new OASISResult<IEnumerable<IAvatar>>(avatars.Result.Where(a =>
                a.Name.Contains(searchTerm.SearchQuery) | a.Description.Contains(searchTerm.SearchQuery)).ToList());

            holons = new OASISResult<IEnumerable<IHolon>>(holons.Result.Where(h =>
                h.Name.Contains(searchTerm.SearchQuery) | h.Description.Contains(searchTerm.SearchQuery)).ToList());

            foreach (var h in holons.Result)
                result.Result.SearchResultHolons.Add((Holon) h);

            foreach (var ava in avatars.Result)
                result.Result.SearchResultHolons.Add((Holon) ava);

            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return LoadHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return await LoadHolonTemplateAsync(a => a.Id == id);
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return LoadHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return await LoadHolonTemplateAsync(a => a.ProviderUniqueStorageKey.Where(b => b.Value == providerKey).Any());
        }

        /*** Templates****/

        public async Task<OASISResult<IAvatar>> LoadAvatarTemplateAsync(Func<HolonResume, bool> predicate)
        {
            string json = "";
            _idLookup = await LoadLookupToJson();

            HolonResume avatarDico = _idLookup.Values.FirstOrDefault(predicate);
            string avatarAddress = _idLookup.FirstOrDefault(a => a.Value.Id == avatarDico.Id).Key;

            json = await LoadStringToJson(avatarAddress);
            IAvatar avatar = JsonConvert.DeserializeObject<Avatar>(json);

            return new OASISResult<IAvatar>(avatar);
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailTemplateAsync(Func<HolonResume, bool> predicate)
        {
            string json = "";
            _idLookup = await LoadLookupToJson();

            HolonResume avatarDico = _idLookup.Values.FirstOrDefault(predicate);
            string avatarAddress = _idLookup.FirstOrDefault(a => a.Value.Id == avatarDico.Id).Key;

            json = await LoadStringToJson(avatarAddress);
            IAvatarDetail avatarDetail = JsonConvert.DeserializeObject<AvatarDetail>(json);

            return new OASISResult<IAvatarDetail>(avatarDetail);
        }


        public async Task<OASISResult<IHolon>> LoadHolonTemplateAsync(Func<HolonResume, bool> predicate)
        {
            string json = "";
            _idLookup = await LoadLookupToJson();

            HolonResume avatarDico = _idLookup.Values.FirstOrDefault(predicate);
            string avatarAddress = _idLookup.FirstOrDefault(a => a.Value.Id == avatarDico.Id).Key;

            json = await LoadStringToJson(avatarAddress);
            IHolon holon = JsonConvert.DeserializeObject<Holon>(json);

            return new OASISResult<IHolon>(holon);
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentTemplateAsync(Func<HolonResume, bool> predicate)
        {
            List<IHolon> holons = new List<IHolon>();
            string json = "";
            _idLookup = await LoadLookupToJson();

            IEnumerable<HolonResume> holonsDico = _idLookup.Values.Where(predicate).AsEnumerable();

            foreach (var h in holonsDico)
            {
                string holonAddress = _idLookup.FirstOrDefault(a => a.Value.Id == h.Id).Key;
                
                json = await LoadStringToJson(holonAddress);
                IHolon holon = JsonConvert.DeserializeObject<Holon>(json);
                holons.Add(holon);
            }

            return new OASISResult<IEnumerable<IHolon>>(holons);
        }
        /***********/

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return await LoadHolonsForParentTemplateAsync(a => a.ParentHolonId == id && a.HolonType == type);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type).Result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IAvatar> avatar = await LoadAvatarTemplateAsync(a => a.Id == id);

                avatar.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                avatar.Result.DeletedDate = DateTime.Now;

                await SaveAvatarToFile(avatar.Result);
                result.Result = true;
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An error occured in DeleteAvatarAsync in IPFSOASIS Provider. Reason: {ex.ToString()}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IAvatar> avatar =
                    await LoadAvatarTemplateAsync(a => a.ProviderUniqueStorageKey.Where(b => b.Value == providerKey).Any());

                avatar.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                avatar.Result.DeletedDate = DateTime.Now;

                await SaveAvatarToFile(avatar.Result);
                result.Result = true;
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An error occured in DeleteAvatarAsync in IPFSOASIS Provider. Reason: {ex.ToString()}");
            }

            return result;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            return DeleteHolonAsync(id, softDelete).Result;
        }

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
        {
            return DeleteHolonAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IHolon> holon = await LoadHolonTemplateAsync(a => a.Id == id);

                holon.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                holon.Result.DeletedDate = DateTime.Now;

                await SaveHolonToFile(holon.Result);

                result.Result = true;
            }
            catch (Exception ex)
            {
                result.Result = true;
                ErrorHandling.HandleError(ref result, $"Error occured in DeleteHolonAsync method in IPFS Provider. Reason: {ex}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IHolon> holon =
                    await LoadHolonTemplateAsync(a => a.ProviderUniqueStorageKey.Where(b => b.Value == providerKey).Any());

                holon.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                holon.Result.DeletedDate = DateTime.Now;

                await SaveHolonToFile(holon.Result);

                result.Result = true;
            }
            catch (Exception ex)
            {
                result.Result = true;
                ErrorHandling.HandleError(ref result, $"Error occured in DeleteHolonAsync method in IPFS Provider. Reason: {ex}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync().Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            List<IAvatar> avatars = new List<IAvatar>();
            string json = "";
            _idLookup = await LoadLookupToJson();

            IEnumerable<HolonResume> Dico = _idLookup.Values.AsEnumerable();

            foreach (var d in Dico)
            {
                string avatarAddress = _idLookup.FirstOrDefault(a => a.Value.Id == d.Id).Key;

                json = await LoadStringToJson(avatarAddress);

                IAvatar avatar = (IAvatar) JsonConvert.DeserializeObject<Avatar>(json);

                avatars.Add(avatar);
            }

            return new OASISResult<IEnumerable<IAvatar>>(avatars.AsEnumerable());
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.Holon, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return LoadAllHolonsAsync(type).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.Holon, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            List<IHolon> HolonsList = new List<IHolon>();
            string json = "";
            _idLookup = await LoadLookupToJson();

            IEnumerable<HolonResume> Dico = _idLookup.Values.AsEnumerable();

            foreach (var d in Dico)
            {
                string HolonAddress = _idLookup.FirstOrDefault(a => a.Value.Id == d.Id).Key;
      
                json = await LoadStringToJson(HolonAddress);
                IHolon holon = JsonConvert.DeserializeObject<Holon>(json);
                HolonsList.Add(holon);
            }

            return new OASISResult<IEnumerable<IHolon>>(HolonsList.Where(a => a.HolonType == type));
        }


        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            return LoadAvatarAsync(Id).Result;
        }

        public override OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        {
            return LoadAvatarAsync(username).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string providerKey, int version = 0)
        {
            return await LoadAvatarTemplateAsync(a => a.ProviderUniqueStorageKey.Where(b => b.Value == providerKey).Any());
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            return await LoadAvatarTemplateAsync(a => a.Id == Id);
        }


        public override OASISResult<IAvatar> LoadAvatarForProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarForProviderKeyAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarForProviderKeyAsync(string providerKey, int version = 0)
        {
            return await LoadAvatarAsync(providerKey);
        }


        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            string json = "";

            result = await LoadHolonsForParentTemplateAsync(a =>
                a.ProviderUniqueStorageKey.Where(a => a.Value == providerKey).Any() && a.HolonType == type);

            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            return await LoadAvatarDetailTemplateAsync(a => a.Id == id);
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync().Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            string json = "";
            json = await LoadStringToJson(avatarDetailsFileAddress);
            AvatarsDetailsList = (List<IAvatarDetail>) JsonConvert.DeserializeObject(json);
            return new OASISResult<IEnumerable<IAvatarDetail>>(AvatarsDetailsList);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            return SaveAvatarDetailAsync(Avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            return new OASISResult<IAvatarDetail>(await SaveAvatarDetailToFile(avatarDetail));
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            return await LoadAvatarTemplateAsync(a => a.email == avatarEmail);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            return await LoadAvatarTemplateAsync(a => a.login == avatarUsername);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail).Result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail).Result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            return await LoadAvatarDetailTemplateAsync(a => a.login == avatarUsername);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            return await LoadAvatarDetailTemplateAsync(a => a.email == avatarEmail);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarEmail).Result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IAvatar> avatar = await LoadAvatarTemplateAsync(a => a.email == avatarEmail);

                avatar.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                avatar.Result.DeletedDate = DateTime.Now;

                await SaveAvatarToFile(avatar.Result);
                result.Result = true;
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An error occured in DeleteAvatarByEmailAsync in IPFSOASIS Provider. Reason: {ex.ToString()}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IAvatar> avatar = await LoadAvatarTemplateAsync(a => a.login == avatarUsername);

                avatar.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                avatar.Result.DeletedDate = DateTime.Now;

                await SaveAvatarToFile(avatar.Result);
                result.Result = true;
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An error occured in DeleteAvatarByUsernameAsync in IPFSOASIS Provider. Reason: {ex.ToString()}");
            }

            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            return SaveHolonAsync(holon, saveChildren).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            OASISResult<IHolon> res = new OASISResult<IHolon>();

            res.Result = await SaveHolonToFile(holon);
            return res;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            return SaveHolonsAsync(holons, saveChildren).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            List<IHolon> savedHolons = new List<IHolon>();

            foreach (var holon in holons)
                savedHolons.Add(await SaveHolonToFile(holon));

            return new OASISResult<IEnumerable<IHolon>>(savedHolons);
        }

        OASISResult<IEnumerable<IPlayer>> IOASISNETProvider.GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> Import(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAll(int version = 0)
        {
            throw new NotImplementedException();
        }
    }
}