using NUnit.Framework;

namespace KitchenChaos.Tests.EditMode
{
    public sealed class SmokeTests
    {
        [Test]
        public void TwoPlusTwo_ReturnsFour()
        {
            int result = 2 + 2;

            Assert.That(result, Is.EqualTo(4));
        }
    }
}
