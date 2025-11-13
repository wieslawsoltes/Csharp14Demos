using Csharp14FeatureSamples.Features;

// Entry point coordinates the execution of each feature demonstration.
var demos = new IFeatureDemo[]
{
    new ExtensionMembersDemo(),
    new FieldKeywordDemo(),
    new ImplicitSpanConversionsDemo(),
    new NameOfUnboundGenericDemo(),
    new SimpleLambdaModifiersDemo(),
    new PartialMembersDemo(),
    new UserDefinedCompoundAssignmentDemo(),
    new NullConditionalAssignmentDemo(),
    new FunctionalExtensionsDemo(),
};

Console.WriteLine("C# 14 Feature Samples");
Console.WriteLine(new string('=', 26));
Console.WriteLine();

foreach (var demo in demos)
{
    DemoRunner.WriteHeader(demo.Title);
    demo.Run();
    Console.WriteLine();
}

/// <summary>
/// Helper utilities to keep the console output consistent across feature demos.
/// </summary>
internal static class DemoRunner
{
    public static void WriteHeader(string title)
    {
        Console.WriteLine($"-- {title} --");
    }
}
