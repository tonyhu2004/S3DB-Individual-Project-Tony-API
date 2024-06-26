﻿using System.Security.Claims;
using Core.Models;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using S3DB_Individual_Project_Tony.CustomFilter;
using S3DB_Individual_Project_Tony.Hub;
using S3DB_Individual_Project_Tony.RequestModels;
using S3DB_Individual_Project_Tony.ViewModels;

namespace S3DB_Individual_Project_Tony.Controllers;

[Authorize]
[ServiceFilter(typeof(CustomExceptionFilter))]
[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private readonly IHubContext<ChatHub, IChatHubClient> _hubContext;
    private readonly ChatService _service;

    public ChatController(ChatService chatService, IHubContext<ChatHub, IChatHubClient> hubContext)
    {
        _service = chatService;
        _hubContext = hubContext;
    }

    [HttpGet("")]
    public IActionResult GetChatBy(string user1Id, string user2Id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var chat = _service.GetOrCreateChatBy(user1Id, user2Id);

        var chatViewModel = new ChatViewModel
        {
            Id = chat.Id,
            CurrentUserId = userId ?? "",
            MessageViewModels = chat?.Messages?.Select(m => new MessageViewModel
            {
                Id = m.Id,
                ChatId = chat.Id,
                SenderUserId = m.SenderUserId,
                Text = m.Text,
                SendDate = m.SendDate
            }).ToList()
        };

        return Ok(chatViewModel);
    }

    [HttpPost("Group/{chatId}/{connectionId}")]
    public async Task<IActionResult> AddToGroup(string chatId, string connectionId)
    {
        await _hubContext.Groups.AddToGroupAsync(connectionId, chatId);
        return Ok();
    }

    [HttpPost("Message")]
    public async Task<IActionResult> SendMessage([FromForm] MessageRequest messageRequest)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null) return Unauthorized();

        var message = new Message
        {
            ChatId = Convert.ToInt32(messageRequest.ChatId),
            Text = messageRequest.Text!,
            SendDate = DateTime.Now,
            SenderUserId = userId
        };

        _service.SendMessage(message);

        await _hubContext.Clients.Group(messageRequest.ChatId).ReceiveMessage(message);

        return Ok();
    }
}