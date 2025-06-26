using System;
using System.Collections.Generic;
using DiscountCodeApplication.Services;
using Xunit;

namespace DiscountCodeApplication.Test
{
    public class DiscountCodeGeneratorTests
    {
        private readonly DiscountCodeGenerator _generator = new();

        [Fact]
        public void GenerateCodes_ReturnsCorrectCount_AndUniqueCodes()
        {
            var codes = _generator.GenerateCodes(10, 7, new List<string>());
            Assert.Equal(10, codes.Count);
            Assert.Equal(10, new HashSet<string>(codes).Count); // All unique
        }

        [Fact]
        public void GenerateCodes_RespectsLength()
        {
            var codes = _generator.GenerateCodes(5, 8, new List<string>());
            Assert.All(codes, code => Assert.Equal(8, code.Length));
        }

        [Fact]
        public void GenerateCodes_ThrowsIfCountIsZero()
        {
            Assert.Throws<ArgumentException>(() => _generator.GenerateCodes(0, 7, new List<string>()));
        }

        [Fact]
        public void GenerateCodes_ThrowsIfCountIsTooLarge()
        {
            Assert.Throws<ArgumentException>(() => _generator.GenerateCodes(2001, 7, new List<string>()));
        }

        [Fact]
        public void GenerateCodes_ThrowsIfLengthTooShort()
        {
            Assert.Throws<ArgumentException>(() => _generator.GenerateCodes(1, 6, new List<string>()));
        }

        [Fact]
        public void GenerateCodes_ThrowsIfLengthTooLong()
        {
            Assert.Throws<ArgumentException>(() => _generator.GenerateCodes(1, 9, new List<string>()));
        }

        [Fact]
        public void GenerateCodes_DoesNotReturnExistingCodes()
        {
            var existing = new List<string> { "ABCDEFG", "HIJKLMN" };
            var codes = _generator.GenerateCodes(5, 7, existing);
            Assert.DoesNotContain("ABCDEFG", codes);
            Assert.DoesNotContain("HIJKLMN", codes);
        }
    }
}