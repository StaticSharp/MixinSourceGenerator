using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace MixinSourceGenerator
{
    internal class MixinData
    {
        public INamedTypeSymbol TargetType { get; set; }

        public IEnumerable<INamedTypeSymbol> MixinTypes { get; set; }
    }
}
