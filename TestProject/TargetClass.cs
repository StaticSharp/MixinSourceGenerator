using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MixinSourceGenerator;

namespace MixinSourceGeneratorTestProject
{
    [Mix(typeof(MixinClass))]
    public partial class TargetClass
    {
    }
}
