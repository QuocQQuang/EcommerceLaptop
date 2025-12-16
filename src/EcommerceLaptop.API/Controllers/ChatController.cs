using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.DTOs.Chat;
using System.Collections.Generic;

namespace EcommerceLaptop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Commented out for easier load testing, or use allow anonymous for specific endpoint if needed
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message is required.");
            }

            // For load testing, we aggregate the stream into a single response 
            // to simulate a request-response cycle and verify completion/correctness.
            
            var fullResponse = new StringBuilder();
            var events = new List<ChatStreamEvent>();

            try 
            {
                await foreach (var chatEvent in _chatService.StreamChatAsync(request.Message, request.SessionId, User.Identity?.Name))
                {
                    events.Add(chatEvent); // Collect events for debugging if needed

                    if (chatEvent is TokenEvent tokenEvent)
                    {
                        fullResponse.Append(tokenEvent.Token);
                    }
                    else if (chatEvent is ErrorEvent errorEvent)
                    {
                        // If it's a non-recoverable error, we might want to return 500 or 400
                        if (!errorEvent.Recoverable)
                        {
                            return StatusCode(503, errorEvent.Message);
                        }
                    }
                }

                return Ok(new 
                { 
                    Response = fullResponse.ToString(),
                    Events = events // Optional: return trace of events
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? SessionId { get; set; }
        public List<object>? History { get; set; } // Optional, for compatibility with existing tests
    }
}
