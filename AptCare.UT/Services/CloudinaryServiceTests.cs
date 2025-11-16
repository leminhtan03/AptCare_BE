using AptCare.Repository.Cloudinary;
using AptCare.Service.Services.Implements;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AptCare.UT.Services
{
    public class CloudinaryServiceTests
    {
        private readonly Mock<IOptions<CloudinarySettings>> _mockOptions;
        private readonly CloudinaryService _service;

        public CloudinaryServiceTests()
        {
            var settings = new CloudinarySettings
            {
                CloudName = "test-cloud",
                ApiKey = "test-key",
                ApiSecret = "test-secret",
                UploadPreset = "test-preset"
            };

            _mockOptions = new Mock<IOptions<CloudinarySettings>>();
            _mockOptions.Setup(o => o.Value).Returns(settings);

            _service = new CloudinaryService(_mockOptions.Object);
        }

        #region UploadImageAsync Tests

        [Fact]
        public async Task UploadImageAsync_WithNullFile_ReturnsNull()
        {
            // Arrange
            IFormFile nullFile = null;

            // Act
            var result = await _service.UploadImageAsync(nullFile);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UploadImageAsync_WithEmptyFile_ReturnsNull()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0);

            // Act
            var result = await _service.UploadImageAsync(mockFile.Object);

            // Assert
            Assert.Null(result);
        }

        // Note: Cannot easily test actual upload without real Cloudinary instance
        // Integration tests would be better for testing actual upload functionality

        #endregion

        #region UploadMultipleImagesAsync Tests

        [Fact]
        public async Task UploadMultipleImagesAsync_WithEmptyList_ReturnsEmptyList()
        {
            // Arrange
            var files = new List<IFormFile>();

            // Act
            var result = await _service.UploadMultipleImagesAsync(files);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task UploadMultipleImagesAsync_WithNullList_ReturnsEmptyList()
        {
            // Arrange
            List<IFormFile> files = null;

            // Act
            var result = await _service.UploadMultipleImagesAsync(files);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task UploadMultipleImagesAsync_FiltersOutNullResults()
        {
            // Arrange
            var mockFile1 = new Mock<IFormFile>();
            mockFile1.Setup(f => f.Length).Returns(0); // Will return null

            var files = new List<IFormFile> { mockFile1.Object };

            // Act
            var result = await _service.UploadMultipleImagesAsync(files);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result); // All null results are filtered out
        }

        #endregion
    }
}