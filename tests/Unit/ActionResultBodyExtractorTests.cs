using System.IO;
using System.Text;
using Byndyusoft.AspNetCore.Instrumentation.Tracing.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.Tests.Unit
{
    public class ActionResultBodyExtractorTests
    {
        private readonly ActionResultBodyExtractor _extractor = new();

        [Fact]
        public void TryExtractBody_NullActionResult()
        {
            // act
            var result = _extractor.TryExtractBody(null, out var body);

            // assert
            Assert.False(result);
            Assert.Null(body);
        }

        [Fact]
        public void TryExtractBody_FileStreamResult()
        {
            // arrange
            var stream = Mock.Of<Stream>();
            var actionResult = new FileStreamResult(stream, "application/json");

            // act
            var result = _extractor.TryExtractBody(actionResult, out var body);

            // assert
            Assert.True(result);
            Assert.Equal("stream", body);
        }

        [Fact]
        public void TryExtractBody_FileContentResult()
        {
            // arrange
            var content = Encoding.UTF8.GetBytes("body");
            var actionResult = new FileContentResult(content, "application/json");

            // act
            var result = _extractor.TryExtractBody(actionResult, out var body);

            // assert
            Assert.True(result);
            Assert.Equal("byte[]", body);
        }

        [Fact]
        public void TryExtractBody_ObjectResult()
        {
            // arrange
            var obj = new {Value = "value"};
            var actionResult = new ObjectResult(obj);

            // act
            var result = _extractor.TryExtractBody(actionResult, out var body);

            // assert
            Assert.True(result);
            Assert.Equal(obj, body);
        }

        [Fact]
        public void TryExtractBody_JsonResult()
        {
            // arrange
            var obj = new {Value = "value"};
            var actionResult = new JsonResult(obj);

            // act
            var result = _extractor.TryExtractBody(actionResult, out var body);

            // assert
            Assert.True(result);
            Assert.Equal(obj, body);
        }

        [Fact]
        public void TryExtractBody_ContentResult()
        {
            // arrange
            var content = "content";
            var actionResult = new ContentResult {Content = content};

            // act
            var result = _extractor.TryExtractBody(actionResult, out var body);

            // assert
            Assert.True(result);
            Assert.Equal(content, body);
        }
    }
}