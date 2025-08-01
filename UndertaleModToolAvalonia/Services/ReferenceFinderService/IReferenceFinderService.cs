﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModToolAvalonia.Models.UndertaleReferenceTypes;

namespace UndertaleModToolAvalonia.Services.ReferenceFinderService
{
    public interface IReferenceFinderService
    {
        public bool IsTypeReferenceable(UndertaleObject undertaleResource);

        (Type, string)[] GetTypeMapForVersion(Type type, UndertaleData data);

        public Task<Dictionary<string, List<object>>?> GetReferencesOfObject(object obj, UndertaleData data, HashSetTypesOverride types, bool checkOne = false);

        Task<Dictionary<string, List<object>>?> GetUnreferencedObjects(
            UndertaleData data, Dictionary<Type, string> typesDict);
    }
}
