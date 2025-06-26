using DiscountCodeApplication.Models;
using DiscountCodeApplication.Repository;
using DiscountCodeApplication.Services.Interfaces;
using DiscountCodeApplication.UnitOfWork;
using Moq;

namespace DiscountCodeApplication.Test
{
    // Add this enum if not available from your main project
    public enum UseCodeResultEnum : byte
    {
        Success = 0,
        Failure = 1,
        AlreadyUsed = 2,
        Expired = 3,
        Inactive = 4,
        Deleted = 5,
        Exception = 6
    }

    public class DiscountCodeServiceTests
    {
        private readonly Mock<IDiscountCodeRepository> _repoMock = new();
        private readonly Mock<IDiscountCodeGenerator> _generatorMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

        private IDiscountCodeService CreateService()
        {
            return new Services.DiscountCodeService(_repoMock.Object, _generatorMock.Object, _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task UseCodeAsync_ReturnsFailure_WhenCodeNotFound()
        {
            _repoMock.Setup(r => r.GetDiscountCodeByCodeAsync("NOTFOUND")).ReturnsAsync((DiscountCode)null);

            var service = CreateService();
            var result = await service.UseCodeAsync("NOTFOUND");

            Assert.Equal((byte)UseCodeResultEnum.Failure, result);
        }

        [Fact]
        public async Task UseCodeAsync_ReturnsDeleted_WhenCodeIsDeleted()
        {
            var code = new DiscountCode { Code = "DELETED", DeletedAt = DateTime.UtcNow };
            _repoMock.Setup(r => r.GetDiscountCodeByCodeAsync("DELETED")).ReturnsAsync(code);

            var service = CreateService();
            var result = await service.UseCodeAsync("DELETED");

            Assert.Equal((byte)UseCodeResultEnum.Deleted, result);
        }

        [Fact]
        public async Task UseCodeAsync_ReturnsInactive_WhenCodeIsInactive()
        {
            var code = new DiscountCode { Code = "INACTIVE", IsActive = false };
            _repoMock.Setup(r => r.GetDiscountCodeByCodeAsync("INACTIVE")).ReturnsAsync(code);

            var service = CreateService();
            var result = await service.UseCodeAsync("INACTIVE");

            Assert.Equal((byte)UseCodeResultEnum.Inactive, result);
        }

        [Fact]
        public async Task UseCodeAsync_ReturnsAlreadyUsed_WhenCodeIsUsed()
        {
            var code = new DiscountCode { Code = "USED", IsUsed = true, IsActive = true };
            _repoMock.Setup(r => r.GetDiscountCodeByCodeAsync("USED")).ReturnsAsync(code);

            var service = CreateService();
            var result = await service.UseCodeAsync("USED");

            Assert.Equal((byte)UseCodeResultEnum.AlreadyUsed, result);
        }

        [Fact]
        public async Task UseCodeAsync_ReturnsExpired_WhenCodeIsExpired()
        {
            var code = new DiscountCode { Code = "EXPIRED", IsActive = true, ExpirationDate = DateTime.UtcNow.AddDays(-1) };
            _repoMock.Setup(r => r.GetDiscountCodeByCodeAsync("EXPIRED")).ReturnsAsync(code);

            var service = CreateService();
            var result = await service.UseCodeAsync("EXPIRED");

            Assert.Equal((byte)UseCodeResultEnum.Expired, result);
        }

        [Fact]
        public async Task UseCodeAsync_ReturnsSuccess_WhenCodeIsValid()
        {
            var code = new DiscountCode
            {
                Code = "VALID",
                IsActive = true,
                IsUsed = false,
                ExpirationDate = DateTime.UtcNow.AddDays(1)
            };
            _repoMock.Setup(r => r.GetDiscountCodeByCodeAsync("VALID")).ReturnsAsync(code);

            var service = CreateService();
            var result = await service.UseCodeAsync("VALID");

            Assert.Equal((byte)UseCodeResultEnum.Success, result);
            _repoMock.Verify(r => r.UpdateDiscountCodeAsync(It.Is<DiscountCode>(c => c.Code == "VALID" && c.IsUsed)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UseCodeAsync_ReturnsException_OnRepositoryException()
        {
            _repoMock.Setup(r => r.GetDiscountCodeByCodeAsync("EX")).ThrowsAsync(new Exception("db error"));

            var service = CreateService();
            var result = await service.UseCodeAsync("EX");

            Assert.Equal((byte)UseCodeResultEnum.Exception, result);
        }
    }
}