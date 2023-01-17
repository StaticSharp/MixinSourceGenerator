public class Program
{
    public static async Task Main()
    {
        var targetProjectPath = Path.Combine(ProjectDirectory.Path, "..\\TestProject\\MixinSourceGenerator.TestProject.csproj");
        //var targetProjectPath = Path.Combine(ProjectDirectory.Path, "..\\..\\StaticSharp\\StaticSharp\\StaticSharp.csproj");
        var outputPath = Path.Combine(Path.GetDirectoryName(targetProjectPath), $".generated/{typeof(MixinSourceGenerator.MixinSourceGenerator).FullName}");
        await RoslynSourceGeneratorLauncher.RoslynSourceGeneratorLauncher.Launch(new MixinSourceGenerator.MixinSourceGenerator(), targetProjectPath, outputPath);
    }
}