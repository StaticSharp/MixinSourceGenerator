using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace MixinSourceGenerator
{
    static class RefKindStatic
    {
        public static string ToParameterPrefix(this RefKind kind)
        {
            switch (kind)
            {
                case RefKind.Out: return "out ";
                case RefKind.Ref: return "ref ";
                case RefKind.In: return "in ";
                case RefKind.None: return string.Empty;
                default: return string.Empty;
            }
        }
    }
}
