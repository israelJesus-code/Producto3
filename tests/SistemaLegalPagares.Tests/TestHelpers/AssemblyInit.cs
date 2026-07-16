using System.Runtime.CompilerServices;
using QuestPDF.Infrastructure;

namespace SistemaLegalPagares.Tests.TestHelpers;

internal static class AssemblyInit
{
    [ModuleInitializer]
    public static void Init()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
}
