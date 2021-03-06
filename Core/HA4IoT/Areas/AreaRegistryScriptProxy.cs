﻿using System;
using HA4IoT.Contracts.Areas;
using HA4IoT.Contracts.Scripting;
using MoonSharp.Interpreter;

namespace HA4IoT.Areas
{
    public class AreaRegistryScriptProxy : IScriptProxy
    {
        private readonly IAreaRegistryService _areaRegistry;

        [MoonSharpHidden]
        public AreaRegistryScriptProxy(IAreaRegistryService areaRegistry)
        {
            _areaRegistry = areaRegistry ?? throw new ArgumentNullException(nameof(areaRegistry));
        }

        [MoonSharpHidden]
        public string Name => "area";

        public void Register(string id)
        {
            _areaRegistry.RegisterArea(id);
        }
    }
}
