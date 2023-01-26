using System.Runtime.InteropServices;



namespace MixinSourceGeneratorTestProject
{
    [Mix(typeof(MixinClass))]
    public partial class TargetClass
    { 
        public void Method1()
        {

        }
    }

    [Mix(typeof(MixinClass))]
    public partial class TargetClass
    {
        public void Method2()
        {

        }
    }

    namespace DifferentNamespace
    {
        [Mix(typeof(MixinClass))]
        public partial class TargetClass
        {
            public void MethodFromDifferentNamespace()
            {

            }
        }
    }
}
