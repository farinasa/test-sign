using Xunit;
using codegentest.Services;

namespace codegentest.Tests
{
    public class CubeServiceTests
    {
        [Fact]
        public void TestCube()
        {
            Assert.Equal(8, CubeService.Cube(2));
        }
    }
}
