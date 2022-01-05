using MovieManager.BusinessLogic;
using MovieManager.Data;
using Shouldly;
using System.Linq;
using Xunit;

namespace MovieManager.Testing
{
    public class FileScannerTest
    {
        [Fact]
        public void ScanFilesTest()
        {
            var fileLocation = @"";
            var xmlEngine = new XmlProcessor();
            var fileScanner = new FileScanner(xmlEngine);
            var movies = fileScanner.ScanFiles(fileLocation);
            movies.Count().ShouldNotBe(0);
        }
    }
}
