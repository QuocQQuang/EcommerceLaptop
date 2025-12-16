using System;
using Xunit;
using EcommerceLaptop.Core.Enums;
using EcommerceLaptop.Infrastructure.Services.AI;
using FluentAssertions;

namespace EcommerceLaptop.UnitTests.Infrastructure.Services.AI
{
    public class SemanticKernelIntentClassifierTests
    {
        [Theory]
        [InlineData("ProductSearch", UserIntent.ProductSearch)]
        [InlineData("productSearch", UserIntent.ProductSearch)] // Case insensitive
        [InlineData("PRODUCTSEARCH", UserIntent.ProductSearch)] // Uppercase
        [InlineData("GeneralChat", UserIntent.GeneralChat)]
        [InlineData("Support", UserIntent.Support)]
        public void ParseIntentFromOutput_ShouldReturnIntent_WhenExactMatch(string input, UserIntent expectedIntent)
        {
            // Act
            var result = SemanticKernelIntentClassifier.ParseIntentFromOutput(input);

            // Assert
            result.Should().Be(expectedIntent);
        }

        [Theory]
        [InlineData("The intent is ProductSearch", UserIntent.ProductSearch)]
        [InlineData("Intent: Support", UserIntent.Support)]
        [InlineData("I believe this is a GeneralChat query", UserIntent.GeneralChat)]
        [InlineData("ProductSearch.", UserIntent.ProductSearch)] // Punctuation
        [InlineData("'ProductSearch'", UserIntent.ProductSearch)] // Quotes
        public void ParseIntentFromOutput_ShouldReturnIntent_WhenRegexFound(string input, UserIntent expectedIntent)
        {
            // Act
            var result = SemanticKernelIntentClassifier.ParseIntentFromOutput(input);

            // Assert
            result.Should().Be(expectedIntent);
        }

        [Theory]
        [InlineData("I don't know", UserIntent.GeneralChat)]
        [InlineData("", UserIntent.GeneralChat)]
        [InlineData("   ", UserIntent.GeneralChat)]
        [InlineData("Something unrelated", UserIntent.GeneralChat)]
        public void ParseIntentFromOutput_ShouldReturnGeneralChat_WhenParsingFails(string input, UserIntent expectedIntent)
        {
            // Act
            var result = SemanticKernelIntentClassifier.ParseIntentFromOutput(input);

            // Assert
            result.Should().Be(expectedIntent);
        }
    }
}
