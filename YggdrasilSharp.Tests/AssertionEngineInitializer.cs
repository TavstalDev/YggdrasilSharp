using FluentAssertions;
using FluentAssertions.Extensibility;
using Tavstal.YggdrasilSharp.Tests;

[assembly: AssertionEngineInitializer(
    typeof(AssertionEngineInitializer),
    nameof(AssertionEngineInitializer.AcknowledgeSoftWarning))]

namespace Tavstal.YggdrasilSharp.Tests;

public static class AssertionEngineInitializer
{
    public static void AcknowledgeSoftWarning()
    {
        License.Accepted = true;
    }
}
