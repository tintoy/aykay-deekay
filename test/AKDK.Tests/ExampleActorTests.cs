using Akka.TestKit;
using Akka.TestKit.Xunit2;
using System;
using Xunit;

namespace AKDK.Tests
{
    public class ExampleActorTests
        : TestKit
    {
        [Fact]
        public void AlwaysPasses()
        {
            Assert.True(true);
        }
    }
}
