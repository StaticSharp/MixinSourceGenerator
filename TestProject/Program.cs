// See https://aka.ms/new-console-template for more information
using MixinSourceGeneratorTestProject;

var t = new TargetClass
{
    MixinProperty = "<Mixin property value>"
};


Console.WriteLine($"Test data: {t.MixinProperty}");