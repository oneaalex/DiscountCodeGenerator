using Moq;
using DiscountCodeApplication.Repository;
using DiscountCodeApplication.Redis;
using DiscountCodeApplication.DB;
using DiscountCodeApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscountCodeApplication.Test
{
    public class CachingDiscountCodeRepositoryTests : IDisposable
    {
        private readonly DiscountCodeContext _context;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly CachingDiscountCodeRepository _repository;

        public CachingDiscountCodeRepositoryTests()
        {
            // Use InMemoryDatabase for EF Core
            var options = new DbContextOptionsBuilder<DiscountCodeContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new DiscountCodeContext(options);
            _mockCacheService = new Mock<ICacheService>();
            _repository = new CachingDiscountCodeRepository(_context, _mockCacheService.Object);
        }

        [Fact]
        public async Task GetDiscountCodeByCodeAsync_CacheMiss_ReturnsCodeFromDbAndCachesIt()
        {
            // Arrange
            string code = "CODE456";
            var expectedCode = new DiscountCode { Code = code, IsActive = true };
            _mockCacheService.Setup(s => s.GetAsync<DiscountCode>($"discountcode:code:{code}", default))
                .ReturnsAsync((DiscountCode?)null);

            _context.DiscountCodes.Add(expectedCode);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetDiscountCodeByCodeAsync(code);

            // Assert
            Assert.Equal(expectedCode.Code, result?.Code);
            _mockCacheService.Verify(s => s.GetAsync<DiscountCode>($"discountcode:code:{code}", default), Times.Once);
            _mockCacheService.Verify(s => s.SetAsync($"discountcode:code:{code}", expectedCode, It.IsAny<TimeSpan>(), default), Times.Once);
        }

        [Fact]
        public async Task GetDiscountCodeByCodeAsync_CodeNotFoundInDb_ThrowsKeyNotFoundException()
        {
            // Arrange
            string code = "NOTFOUND";
            _mockCacheService.Setup(s => s.GetAsync<DiscountCode>($"discountcode:code:{code}", default))
                .ReturnsAsync((DiscountCode?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _repository.GetDiscountCodeByCodeAsync(code));
            _mockCacheService.Verify(s => s.GetAsync<DiscountCode>($"discountcode:code:{code}", default), Times.Once);
        }

        [Fact]
        public async Task AddRangeDiscountCodeAsync_AddsCodesAndUpdatesCache()
        {
            // Arrange
            var codes = new List<DiscountCode>
            {
                new() { Code = "ADD1", IsActive = true },
                new() { Code = "ADD2", IsActive = true }
            };

            // Act
            await _repository.AddRangeDiscountCodeAsync(codes);

            // Assert
            Assert.Equal(2, await _context.DiscountCodes.CountAsync());
            _mockCacheService.Verify(c => c.SetAsync("recent_discount_codes", It.IsAny<List<DiscountCode>>(), It.IsAny<TimeSpan>(), default), Times.Once);
        }

        [Fact]
        public async Task UpdateDiscountCodeAsync_UpdatesAndInvalidatesCache()
        {
            // Arrange
            var code = new DiscountCode { Code = "UPD", IsActive = true };
            _context.DiscountCodes.Add(code);
            await _context.SaveChangesAsync();

            code.IsActive = false;
            await _repository.UpdateDiscountCodeAsync(code);

            var updated = await _context.DiscountCodes.FindAsync("UPD");
            Assert.False(updated?.IsActive);
            _mockCacheService.Verify(c => c.RemoveAsync("discountcode:UPD", default), Times.Once);
            _mockCacheService.Verify(c => c.RemoveAsync("discountcode:code:UPD", default), Times.Once);
            _mockCacheService.Verify(c => c.SetAsync("recent_discount_codes", It.IsAny<List<DiscountCode>>(), It.IsAny<TimeSpan>(), default), Times.Once);
        }

        [Fact]
        public async Task DeleteDiscountCodeAsync_DeletesAndInvalidatesCache()
        {
            // Arrange
            var code = new DiscountCode { Code = "DEL" };
            _context.DiscountCodes.Add(code);
            await _context.SaveChangesAsync();

            await _repository.DeleteDiscountCodeAsync(code);

            Assert.Null(await _context.DiscountCodes.FindAsync("DEL"));
            _mockCacheService.Verify(c => c.RemoveAsync("discountcode:DEL", default), Times.Once);
            _mockCacheService.Verify(c => c.RemoveAsync("discountcode:code:DEL", default), Times.Once);
            _mockCacheService.Verify(c => c.SetAsync("recent_discount_codes", It.IsAny<List<DiscountCode>>(), It.IsAny<TimeSpan>(), default), Times.Once);
        }

        [Fact]
        public async Task GetRecentDiscountCodesAsync_ReturnsFromCache_IfPresent()
        {
            // Arrange
            var expected = new List<DiscountCode> { new() { Code = "RECENT" } };
            _mockCacheService.Setup(c => c.GetAsync<List<DiscountCode>>("recent_discount_codes", default))
                .ReturnsAsync(expected);

            // Act
            var result = await _repository.GetRecentDiscountCodesAsync();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetRecentDiscountCodesAsync_ReturnsFromDb_IfCacheMiss()
        {
            // Arrange
            _mockCacheService.Setup(c => c.GetAsync<List<DiscountCode>>("recent_discount_codes", default))
                .ReturnsAsync((List<DiscountCode>?)null);
            _context.DiscountCodes.Add(new DiscountCode { Code = "RECENT2", CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetRecentDiscountCodesAsync();

            // Assert
            Assert.Single(result);
            _mockCacheService.Verify(c => c.SetAsync("recent_discount_codes", It.IsAny<List<DiscountCode>>(), It.IsAny<TimeSpan>(), default), Times.Once);
        }

        [Fact]
        public async Task GetAllCodesAsync_ReturnsFromCache_IfPresent()
        {
            // Arrange
            var expected = new List<string> { "A", "B" };
            _mockCacheService.Setup(c => c.GetAsync<List<string>>("all_discount_codes", default))
                .ReturnsAsync(expected);

            // Act
            var result = await _repository.GetAllCodesAsync();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetAllCodesAsync_ReturnsFromDb_IfCacheMiss()
        {
            // Arrange
            _mockCacheService.Setup(c => c.GetAsync<List<string>>("all_discount_codes", default))
                .ReturnsAsync((List<string>?)null);
            _context.DiscountCodes.Add(new DiscountCode { Code = "C" });
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllCodesAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("C", result[0]);
            _mockCacheService.Verify(c => c.SetAsync("all_discount_codes", It.IsAny<List<string>>(), It.IsAny<TimeSpan>(), default), Times.Once);
        }

        [Fact]
        public async Task GetMostRecentCodesAsync_ThrowsOnInvalidCount()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetMostRecentCodesAsync(0));
        }

        [Fact]
        public async Task PreloadDiscountCodeCachesAsync_PreloadsCaches()
        {
            // Arrange
            _context.DiscountCodes.Add(new DiscountCode { Code = "PRELOAD", CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            await _repository.PreloadDiscountCodeCachesAsync();

            // Assert
            _mockCacheService.Verify(c => c.SetAsync("recent_discount_codes", It.IsAny<List<DiscountCode>>(), It.IsAny<TimeSpan>(), default), Times.Once);
            _mockCacheService.Verify(c => c.SetAsync("all_discount_codes", It.IsAny<List<string>>(), It.IsAny<TimeSpan>(), default), Times.Once);
        }

        void IDisposable.Dispose() => _context.Dispose();
    }
}