using System;
using System.Linq;
using System.Threading.Tasks;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Infrastructure.Services.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EcommerceLaptop.IntegrationTests.Services
{
    public class ChatPersistenceServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private IDistributedCache GetMemoryDistributedCache()
        {
            var opts = Options.Create(new MemoryDistributedCacheOptions());
            return new MemoryDistributedCache(opts);
        }

        [Fact]
        public async Task CreateSessionAsync_ShouldCreateSession_InDatabase()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var cache = GetMemoryDistributedCache();
            var service = new ChatPersistenceService(context, cache);

            // Act
            var sessionId = await service.CreateSessionAsync("user123", "Test Session");

            // Assert
            var session = await context.ChatSessions.FindAsync(int.Parse(sessionId));
            Assert.NotNull(session);
            Assert.Equal("user123", session.UserId);
            Assert.Equal("Test Session", session.Title);
        }

        [Fact]
        public async Task SaveMessageAsync_ShouldPersistMessage_AndReflectInHistory()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var cache = GetMemoryDistributedCache();
            var service = new ChatPersistenceService(context, cache);
            var sessionId = await service.CreateSessionAsync("user123", "Test Session");

            // Act
            await service.SaveMessageAsync(sessionId, "user", "Hello World");
            await service.SaveMessageAsync(sessionId, "assistant", "Hi there!");

            // Assert (DB)
            var messages = await context.ChatMessages.Where(m => m.SessionId == int.Parse(sessionId)).ToListAsync();
            Assert.Equal(2, messages.Count);
            Assert.Contains(messages, m => m.Content == "Hello World" && m.Role == "user");
            Assert.Contains(messages, m => m.Content == "Hi there!" && m.Role == "assistant");

            // Act (Read Service)
            var history = await service.GetSessionHistoryAsync(sessionId);
            Assert.Equal(2, history.Count());
        }

        [Fact]
        public async Task GetSessionHistoryAsync_ShouldUseCache_WhenAvailable()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockCache = new Mock<IDistributedCache>();
            
            // Setup Cache to return dummy data
            // We'll skip complex mock setup for IDistributedCache extension methods as they are static.
            // Instead, we verify behavior by using the Real Memory Cache and inspecting behavior indirectly or by trusting the Logic.
            // For IntegrationTest, using Real MemoryCache is better to verify the full flow.
            
            var cache = GetMemoryDistributedCache();
            var service = new ChatPersistenceService(context, cache);
            var sessionId = await service.CreateSessionAsync("user123", "Caching Test");
            
            await service.SaveMessageAsync(sessionId, "user", "Message 1");
            
            // First call -> loads from DB -> writes to Cache
            var history1 = await service.GetSessionHistoryAsync(sessionId);
            Assert.Single(history1);
            
            // Manipulate DB behind the scenes (delete message) to prove we define reading from Cache
            var msg = await context.ChatMessages.FirstAsync();
            context.ChatMessages.Remove(msg);
            await context.SaveChangesAsync();
            
            // Second call -> should still return 1 message (from cache)
            var history2 = await service.GetSessionHistoryAsync(sessionId);
            Assert.Single(history2);
            Assert.Equal("Message 1", history2.First().Content);
        }

        [Fact]
        public async Task DeleteSessionAsync_ShouldRemoveFromDb_AndInvalidateCache()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var cache = GetMemoryDistributedCache();
            var service = new ChatPersistenceService(context, cache);
            var sessionId = await service.CreateSessionAsync("user123", "Delete Test");
            await service.SaveMessageAsync(sessionId, "user", "msg");

            // Ensure cached
            await service.GetSessionHistoryAsync(sessionId);

            // Act
            await service.DeleteSessionAsync(sessionId, "user123");

            // Assert
            var session = await context.ChatSessions.FindAsync(int.Parse(sessionId));
            Assert.Null(session);

            // Verify cache is cleared (by checking if history is empty or re-fetched empty from DB)
            var history = await service.GetSessionHistoryAsync(sessionId);
            Assert.Empty(history);
        }
    }
}
