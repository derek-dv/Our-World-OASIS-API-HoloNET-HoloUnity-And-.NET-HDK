﻿using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    //TODO: Need to upgrade all methods to return OASISResult wrapper ASAP! :)
    public class ProviderManager
    {
        private static List<IOASISProvider> _registeredProviders = new List<IOASISProvider>();
        private static List<EnumValue<ProviderType>> _registeredProviderTypes = new List<EnumValue<ProviderType>>();
        private static List<EnumValue<ProviderType>> _providerAutoFailOverList { get; } = new List<EnumValue<ProviderType>>();
        private static List<EnumValue<ProviderType>> _providerAutoLoadBalanceList { get; } = new List<EnumValue<ProviderType>>();
        private static List<EnumValue<ProviderType>> _providersThatAreAutoReplicating { get; } = new List<EnumValue<ProviderType>>();
        private static bool _setProviderGlobally = false;

        public static EnumValue<ProviderType> CurrentStorageProviderType { get; private set; } = new EnumValue<ProviderType>(ProviderType.Default);
        public static EnumValue<ProviderCategory> CurrentStorageProviderCategory { get; private set; } = new EnumValue<ProviderCategory>(ProviderCategory.None);
        public static OASISProviderBootType OASISProviderBootType { get; set; } = OASISProviderBootType.Hot;

        public static bool IsAutoReplicationEnabled { get; set; } = true;
        public static bool IsAutoFailOverEnabled { get; set; } = true;
        public static bool IsAutoLoadBalanceEnabled { get; set; } = true;

        //public static string CurrentStorageProviderName
        //{
        //    get
        //    {
        //        return Enum.GetName(CurrentStorageProviderType);
        //    }
        //}

        // public static string[] DefaultProviderTypes { get; set; }

        public static IOASISStorageProvider DefaultGlobalStorageProvider { get; set; }

        public static IOASISStorageProvider CurrentStorageProvider { get; private set; } //TODO: Need to work this out because in future there can be more than one provider active at a time.

        public static bool OverrideProviderType { get; set; } = false;

       
        

        //public static List<ProviderType> RegisteredProviderTypes
        //{
        //    get
        //    {
        //        if (_registeredProviderTypes == null)
        //        {
        //            _registeredProviderTypes = new List<ProviderType>();
        //        }
        //    }
        //}

        //TODO: In future the registered providers will be dynamically loaded from MEF by watching a hot folder for compiled provider dlls (and other ways in future...)
        public static bool RegisterProvider(IOASISProvider provider)
        {
            if (!_registeredProviders.Any(x => x.ProviderType == provider.ProviderType))
            {
                _registeredProviders.Add(provider);
                _registeredProviderTypes.Add(provider.ProviderType);
                return true;
            }

            return false;
        }

        public static bool RegisterProviders(List<IOASISProvider> providers)
        {
            bool returnValue = false;

            foreach (IOASISProvider provider in providers)
                returnValue = RegisterProvider(provider);

            return returnValue;
        }

        public static bool UnRegisterProvider(IOASISProvider provider)
        {
            DeActivateProvider(provider);

            _registeredProviders.Remove(provider);
            _registeredProviderTypes.Remove(provider.ProviderType);
            return true;
        }

        public static bool UnRegisterProvider(ProviderType providerType)
        {
            foreach (IOASISProvider provider in _registeredProviders)
            {
                if (provider.ProviderType.Value == providerType)
                {
                    UnRegisterProvider(provider);
                    break;
                }
            }    
            
            return true;
        }

        public static bool UnRegisterProviders(List<ProviderType> providerTypes)
        {
            foreach (ProviderType providerType in providerTypes)
                UnRegisterProvider(providerType);

            return true;
        }

        public static bool UnRegisterProviders(List<IOASISProvider> providers)
        {
            foreach (IOASISProvider provider in providers)
                _registeredProviders.Remove(provider);

            return true;
        }

        public static ProviderCategory GetProviderCategory(ProviderType providerType)
        {
            foreach (IOASISProvider provider in _registeredProviders)
            {
                if (provider.ProviderType.Value == providerType)
                    return provider.ProviderCategory.Value;
            }

            return ProviderCategory.None;
        }

        public static List<IOASISProvider> GetAllRegisteredProviders()
        {
            return _registeredProviders;
        }

        public static List<EnumValue<ProviderType>> GetAllRegisteredProviderTypes()
        {
            return _registeredProviderTypes;
        }

        public static List<IOASISProvider> GetProvidersOfCategory(ProviderCategory category)
        {
            return _registeredProviders.Where(x => x.ProviderCategory.Value == category).ToList();
        }

        public static List<ProviderType> GetProviderTypesOfCategory(ProviderCategory category)
        {
            return GetProviderTypes(GetProvidersOfCategory(category));
        }

        public static List<IOASISStorageProvider> GetStorageProviders()
        {
            List<IOASISStorageProvider> storageProviders = new List<IOASISStorageProvider>();

            foreach (IOASISProvider provider in _registeredProviders.Where(x => x.ProviderCategory.Value == ProviderCategory.Storage || x.ProviderCategory.Value == ProviderCategory.StorageAndNetwork || x.ProviderCategory.Value == ProviderCategory.StorageLocal || x.ProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork).ToList())
                storageProviders.Add((IOASISStorageProvider)provider);  

            return storageProviders;
        }

        public static List<ProviderType> GetStorageProviderTypes()
        {
            //return GetProviderTypes(GetStorageProviders().Select(x => x.ProviderType);
            return GetStorageProviders().Select(x => x.ProviderType.Value).ToList();
        }

        public static List<IOASISNETProvider> GetNetworkProviders()
        {
            List<IOASISNETProvider> networkProviders = new List<IOASISNETProvider>();

            foreach (IOASISProvider provider in _registeredProviders.Where(x => x.ProviderCategory.Value == ProviderCategory.Network || x.ProviderCategory.Value == ProviderCategory.StorageAndNetwork || x.ProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork).ToList())
                networkProviders.Add((IOASISNETProvider)provider);

            return networkProviders;
        }

        public static List<ProviderType> GetNetworkProviderTypes()
        {
            return GetNetworkProviders().Select(x => x.ProviderType.Value).ToList();

            //List<ProviderType> providerTypes = new List<ProviderType>();

            //foreach (IOASISProvider provider in GetNetworkProviders())
            //    providerTypes.Add(provider.ProviderType);

            //return providerTypes;
        }

        public static List<ProviderType> GetProviderTypes(List<IOASISProvider> providers)
        {
            List<ProviderType> providerTypes = new List<ProviderType>();

            foreach (IOASISProvider provider in providers)
                providerTypes.Add(provider.ProviderType.Value);

            return providerTypes;
        }

        public static List<IOASISRenderer> GetRendererProviders()
        {
            List<IOASISRenderer> rendererProviders = new List<IOASISRenderer>();

            foreach (IOASISProvider provider in _registeredProviders.Where(x => x.ProviderCategory.Value == ProviderCategory.Renderer).ToList())
                rendererProviders.Add((IOASISRenderer)provider);

            return rendererProviders;
        }

        public static IOASISProvider GetProvider(ProviderType type)
        {
            return _registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type);
        }

        public static IOASISStorageProvider GetStorageProvider(ProviderType type)
        {
            return (IOASISStorageProvider)_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type);
            //return (IOASISStorageProvider)_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type && x.ProviderCategory.Value == ProviderCategory.Storage);
        }

        public static IOASISNETProvider GetNetworkProvider(ProviderType type)
        {
            return (IOASISNETProvider)_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type);
            //return (IOASISNETProvider)_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type && x.ProviderCategory.Value == ProviderCategory.Network);
        }

        public static IOASISRenderer GetRendererProvider(ProviderType type)
        {
            return (IOASISRenderer)_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type);
            //return (IOASISRenderer)_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type && x.ProviderCategory.Value == ProviderCategory.Renderer);
        }

        public static bool IsProviderRegistered(IOASISProvider provider)
        {
            return _registeredProviders.Any(x => x.ProviderName == provider.ProviderName);
        }

        public static bool IsProviderRegistered(ProviderType providerType)
        {
            return _registeredProviders.Any(x => x.ProviderType.Value == providerType);
        }

        //public static IOASISSuperStar SetAndActivateCurrentSuperStarProvider(ProviderType providerType)
        //{
        //    SetAndActivateCurrentStorageProvider(providerType);

            
      //  }

        // Called from Managers.
        public static OASISResult<IOASISStorageProvider> SetAndActivateCurrentStorageProvider(ProviderType providerType)
        {
            OASISResult<IOASISStorageProvider> result = new OASISResult<IOASISStorageProvider>();

            if (providerType == ProviderType.Default)
                result = SetAndActivateCurrentStorageProvider();
            else
                result = SetAndActivateCurrentStorageProvider(providerType, false);

            if (result.IsError)
                result.Message = string.Concat("ERROR: The ", Enum.GetName(providerType), " provider is not registered. Please register it before calling this method. Reason: ", result.Message);

            return result;
        }

        //TODO: Called internally (make private ?)
        public static OASISResult<IOASISStorageProvider> SetAndActivateCurrentStorageProvider()
        {
            // If a global provider has been set and the REST API call has not overiden the provider (OverrideProviderType) then set to global provider.
            if (DefaultGlobalStorageProvider != null && DefaultGlobalStorageProvider != CurrentStorageProvider && !OverrideProviderType)
                return SetAndActivateCurrentStorageProvider(DefaultGlobalStorageProvider);

            // Otherwise set to default provider (configured in appSettings.json) if the provider has not been overiden in the REST call.
            //else if (!OverrideProviderType && DefaultProviderTypes != null && CurrentStorageProviderType.Value != (ProviderType)Enum.Parse(typeof(ProviderType), DefaultProviderTypes[0]))
            else if (!OverrideProviderType && _providerAutoFailOverList.Count > 0 && CurrentStorageProviderType.Value != _providerAutoFailOverList[0].Value) // TODO: Come back to this, not sure we should be setting the first entry every time? Needs thinking and testing through! ;-)
                return SetAndActivateCurrentStorageProvider(ProviderType.Default, false);

            if (!_setProviderGlobally)
                OverrideProviderType = false;

            return new OASISResult<IOASISStorageProvider>(CurrentStorageProvider);
        }

        // Called from ONODE.WebAPI.OASISProviderManager.
        public static OASISResult<IOASISStorageProvider> SetAndActivateCurrentStorageProvider(IOASISProvider OASISProvider)
        {
            if (OASISProvider != CurrentStorageProvider)
            {
                if (OASISProvider != null)
                {
                    if (!IsProviderRegistered(OASISProvider))
                        RegisterProvider(OASISProvider);

                    return SetAndActivateCurrentStorageProvider(OASISProvider.ProviderType.Value);
                }
            }

            return new OASISResult<IOASISStorageProvider>(CurrentStorageProvider);
        }

        // Called from ONODE.WebAPI.OASISProviderManager.
        //TODO: In future more than one StorageProvider will be active at a time so we need to work out how to handle this...
        public static OASISResult<IOASISStorageProvider> SetAndActivateCurrentStorageProvider(ProviderType providerType, bool setGlobally = false)
        {
            OASISResult<IOASISStorageProvider> result = new OASISResult<IOASISStorageProvider>();
            _setProviderGlobally = setGlobally;

            // TODO: Need to get this to use the next provider in the list if there is an issue with the first/current provider...
            // This is automatically handled in the Managers (AvatarManager, HolonManager, etc) whenever a provider throws an exception, it will try the next provider in the list... :)
            if (providerType == ProviderType.Default && !OverrideProviderType && _providerAutoFailOverList.Count > 0)
                providerType = _providerAutoFailOverList[0].Value;
                //providerType = (ProviderType)Enum.Parse(typeof(ProviderType), DefaultProviderTypes[0]);

            if (providerType != CurrentStorageProviderType.Value)
            {
                IOASISProvider provider = _registeredProviders.FirstOrDefault(x => x.ProviderType.Value == providerType);

                //TODO: Use OASISResult instead.
                if (provider == null)
                    throw new InvalidOperationException(string.Concat(Enum.GetName(typeof(ProviderType), providerType), " ProviderType is not registered. Please call RegisterProvider() method to register the provider before calling this method."));

                if (provider != null && (provider.ProviderCategory.Value == ProviderCategory.Storage 
                    || provider.ProviderCategory.Value == ProviderCategory.StorageAndNetwork 
                    || provider.ProviderCategory.Value == ProviderCategory.StorageLocal 
                    || provider.ProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork))
                {
                    if (CurrentStorageProvider != null)
                    {
                        OASISResult<bool> deactivateProviderResult = DeActivateProvider(CurrentStorageProvider);

                        if (deactivateProviderResult.IsError)
                        {
                           // result.IsError = true; // TODO: Think its not an error as long as it can activate a provider below?
                            result.Message = deactivateProviderResult.Message;
                        }
                    }

                    CurrentStorageProviderCategory = provider.ProviderCategory;
                    CurrentStorageProviderType.Value = providerType;
                    CurrentStorageProvider = (IOASISStorageProvider)provider;

                    OASISResult<bool> activateProviderResult = ActivateProvider(CurrentStorageProvider);

                    if (activateProviderResult.IsError)
                    {
                        result.IsError = true;
                        result.Message = activateProviderResult.Message;
                    }

                    if (setGlobally)
                        DefaultGlobalStorageProvider = CurrentStorageProvider;
                }
            }

            result.Result = CurrentStorageProvider;
            return result;
        }

        public static OASISResult<bool> ActivateProvider(ProviderType type)
        {
            return ActivateProvider(_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type));
        }

        public static OASISResult<bool> ActivateProvider(IOASISProvider provider)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            if (provider != null)
            {
                try
                {
                    result = provider.ActivateProvider();
                }
                catch (Exception ex)
                {
                    ErrorHandling.HandleError(ref result, string.Concat("Error Activating Provider ", provider.ProviderType.Name, ". Reason: ", ex.ToString()));
                }
            }
            else
                ErrorHandling.HandleError(ref result, "Error Activating Provider. Provider passed in is null!");

            return result;
        }

        public static OASISResult<bool> DeActivateProvider(ProviderType type)
        {
            return DeActivateProvider(_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type));
        }

        public static OASISResult<bool> DeActivateProvider(IOASISProvider provider)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            if (provider != null)
            {
                try
                {
                    result = provider.DeActivateProvider();
                }
                catch (Exception ex)
                {
                    ErrorHandling.HandleError(ref result, string.Concat("Error DeActivating Provider ", provider.ProviderType.Name, ". Reason: ", ex.ToString()));
                }
            }
            else
                ErrorHandling.HandleError(ref result, "Error DeActivating Provider. Provider passed in is null!");

            return result;
        }

        public static bool SetAutoReplicationForProviders(bool autoReplicate, List<ProviderType> providers)
        {
            return SetProviderList(autoReplicate, providers, _providersThatAreAutoReplicating);
        }

        public static bool SetAutoReplicateForAllProviders(bool autoReplicate)
        {
            return SetAutoReplicationForProviders(autoReplicate, _registeredProviderTypes.Select(x => x.Value).ToList());
        }

        public static bool SetAutoFailOverForProviders(bool addToFailOverList, List<ProviderType> providers)
        {
            return SetProviderList(addToFailOverList, providers, _providerAutoFailOverList);
        }

        public static bool SetAutoFailOverForAllProviders(bool addToFailOverList)
        {
            return SetAutoFailOverForProviders(addToFailOverList, _registeredProviderTypes.Select(x => x.Value).ToList());
        }

        public static bool SetAutoLoadBalanceForProviders(bool addToLoadBalanceList, List<ProviderType> providers)
        {
            return SetProviderList(addToLoadBalanceList, providers, _providerAutoLoadBalanceList);
        }

        public static bool SetAutoLoadBalanceForAllProviders(bool addToLoadBalanceList)
        {
            return SetAutoLoadBalanceForProviders(addToLoadBalanceList, _registeredProviderTypes.Select(x => x.Value).ToList());
        }

        public static List<EnumValue<ProviderType>> GetProviderAutoLoadBalanceList()
        {
            return _providerAutoLoadBalanceList;
        }

        public static List<EnumValue<ProviderType>> GetProviderAutoFailOverList()
        {
            return _providerAutoFailOverList;
        }

        public static string GetProviderAutoFailOverListAsString()
        {
            return GetProviderListAsString(GetProviderAutoFailOverList());
        }
        public static string GetProvidersThatAreAutoReplicatingAsString()
        {
            return GetProviderListAsString(GetProvidersThatAreAutoReplicating());
        }
        public static string GetProviderAutoLoadBalanceListAsString()
        {
            return GetProviderListAsString(GetProviderAutoLoadBalanceList());
        }

        private static string GetProviderListAsString(List<EnumValue<ProviderType>> providerList)
        {
            string list = "";

            for (int i = 0; i < providerList.Count(); i++)
            {
                list = string.Concat(list, providerList[i].Name);

                if (i < providerList.Count - 2)
                    list = string.Concat(list, ", ");

                else if (i == providerList.Count() - 2)
                    list = string.Concat(list, " & ");
            }

            return list;
        }

        public static List<EnumValue<ProviderType>> GetProvidersThatAreAutoReplicating()
        {
            return _providersThatAreAutoReplicating;
        }

        private static bool SetProviderList(bool add, List<ProviderType> providers, List<EnumValue<ProviderType>> listToAddTo)
        {
            foreach (ProviderType providerType in providers)
            {
                if (add && !listToAddTo.Any(x => x.Value == providerType))
                    listToAddTo.Add(new EnumValue<ProviderType>(providerType));

                else if (!add)
                {
                    foreach (EnumValue<ProviderType> type in listToAddTo)
                    {
                        if (type.Value == providerType)
                        {
                            listToAddTo.Remove(type);
                            break;
                        }
                    }
                }
            }

            return true;
        }
    }
}
