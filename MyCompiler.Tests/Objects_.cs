using MyCompiler.Entities;

namespace MyCompiler.Tests
{
    public class Objects_
    {
        [Theory]
        [InlineData("Hello World", "Hello World", true)]
        [InlineData("My name is johnny", "My name is johnny", true)]
        [InlineData("Hello World", "My name is johnny", false)]
        public void Hashes_strings_uniquely(string arg0, string arg1, bool expected)
        {
            var first = new StringObject { Value = arg0 }.HashKey();
            var second = new StringObject { Value = arg1 }.HashKey();
            Assert.Equal(expected, first.Equals(second));
        }
    }
}