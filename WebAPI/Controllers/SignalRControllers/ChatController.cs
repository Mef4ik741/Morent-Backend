using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Application.Cloudinary;
using WebAPI.Application.DTOs;
using WebAPI.Application.Hubs;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;
using WebAPI.Domain.Models;
using WebAPI.Infrastructure.Data.Context;

namespace WebAPI.Controllers.SignalRControllers;

[ApiController]
[Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly Context _context;
        private readonly IHubContext<ChatHub> _hub;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IUserService _userService;
        
        public ChatController(Context context, IHubContext<ChatHub> hub, ICloudinaryService cloudinaryService, IUserService userService)
        {
            _context = context;
            _hub = hub;
            _cloudinaryService = cloudinaryService;
            _userService = userService;
        }

        [HttpPut("messages/{id}")]
        public async Task<ActionResult<ChatMessage>> UpdateMessage(int id, [FromBody] UpdateMessageDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.UserId))
                return BadRequest("UserId and Message are required");

            var msg = await _context.ChatMessages.FirstOrDefaultAsync(m => m.Id == id);
            if (msg == null || msg.IsDeleted)
                return NotFound("Message not found");

            if (msg.FromUserId != dto.UserId)
                return Forbid();

            if (string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest("Message text cannot be empty");

            msg.Message = dto.Message.Trim();
            msg.IsEdited = true;
            msg.EditedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await ChatHub.BroadcastMessageEdited(_hub, id, msg.Message, msg.ToUserId, msg.FromUserId);

            return Ok(msg);
        }

        [HttpPost("messages/{id}/delete")]
        public async Task<IActionResult> SoftDeleteMessage(int id, [FromBody] DeleteMessageDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.UserId))
                return BadRequest("UserId is required");

            var msg = await _context.ChatMessages.FirstOrDefaultAsync(m => m.Id == id);
            if (msg == null)
                return NotFound("Message not found");

            if (msg.IsDeleted)
                return NoContent();

            if (msg.FromUserId != dto.UserId)
                return Forbid();

            msg.IsDeleted = true;
            await _context.SaveChangesAsync();

            await ChatHub.BroadcastMessageDeleted(_hub, id, msg.ToUserId, msg.FromUserId);
            
            return NoContent();
        }

        [HttpGet("unread/{userId}")]
        public async Task<ActionResult<UnreadCountDTO>> GetUnreadCount(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("userId is required");

            var count = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.ToUserId == userId && !m.IsRead && !m.IsDeleted)
                .CountAsync();

            return Ok(new UnreadCountDTO(userId, count));
        }

        [HttpGet("inbox/senders/{userId}")]
        public async Task<ActionResult<List<ChatUserBriefDTO>>> GetInboxSenders(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("userId is required");

            var senderIds = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.ToUserId == userId && !m.IsDeleted)
                .Select(m => m.FromUserId)
                .Distinct()
                .ToListAsync();

            if (senderIds.Count == 0)
                return Ok(new List<ChatUserBriefDTO>());

            var senders = await _context.Users
                .AsNoTracking()
                .Where(u => senderIds.Contains(u.Id))
                .Select(u => new ChatUserBriefDTO(u.Id, u.Username, u.ImageProfileURL))
                .ToListAsync();

            return Ok(senders);
        }

        [HttpGet("users")]
        public async Task<ActionResult<List<ChatUserDTO>>> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new ChatUserDTO(
                    u.Id,
                    u.Username,
                    u.Name,
                    u.Surname,
                    false
                ))
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("messages/{userId1}/{userId2}")]
        public async Task<ActionResult<List<ChatMessage>>> GetMessages(string userId1, string userId2)
        {
            var messages = await _context.ChatMessages
                .Where(m => (m.FromUserId == userId1 && m.ToUserId == userId2) ||
                           (m.FromUserId == userId2 && m.ToUserId == userId1))
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return Ok(messages);
        }


        [HttpPost("messages")]
        public async Task<ActionResult<ChatMessage>> SaveMessage([FromBody] SendMessageDto messageDto)
        {
            await UpdateOrCreateConversation(messageDto.FromUserId, messageDto.ToUserId);
            await _context.SaveChangesAsync();

            var message = new ChatMessage
            {
                FromUserId = messageDto.FromUserId,
                ToUserId = messageDto.ToUserId,
                Message = messageDto.Message,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(message);
        }

        [HttpPost("messages/owner")]
        public async Task<ActionResult<ChatMessage>> SendMessageToOwner([FromBody] SendMessageToOwnerDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FromUserId) || string.IsNullOrWhiteSpace(dto.OwnerUserId))
            {
                return BadRequest("FromUserId and OwnerUserId are required.");
            }

            if (dto.FromUserId == dto.OwnerUserId)
            {
                return BadRequest("You cannot send a message to yourself.");
            }

            var fromExists = await _context.Users.AnyAsync(u => u.Id == dto.FromUserId);
            var ownerExists = await _context.Users.AnyAsync(u => u.Id == dto.OwnerUserId);
            if (!fromExists || !ownerExists)
            {
                return NotFound("One or both users not found.");
            }

            await UpdateOrCreateConversation(dto.FromUserId, dto.OwnerUserId);
            await _context.SaveChangesAsync();

            var message = new ChatMessage
            {
                FromUserId = dto.FromUserId,
                ToUserId = dto.OwnerUserId,
                Message = dto.Message,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(message);
        }

        [HttpPost("mark-read")]
        public async Task<ActionResult> MarkMessagesAsRead([FromBody] MarkAsReadRequest request)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.ToUserId == request.UserId1 && 
                           m.FromUserId == request.UserId2 && 
                           !m.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("conversations/{userId}")]
        public async Task<ActionResult<List<ConversationDTO>>> GetUserConversations(string userId)
        {
            var conversations = await _context.ChatConversations
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.LastMessageTime)
                .ToListAsync();

            var conversationDtos = new List<ConversationDTO>();

            foreach (var conversation in conversations)
            {
                var otherUserId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;
                var otherUser = await _context.Users.FindAsync(otherUserId);
                
                var lastMessage = await _context.ChatMessages
                    .Where(m => (m.FromUserId == conversation.User1Id && m.ToUserId == conversation.User2Id) ||
                               (m.FromUserId == conversation.User2Id && m.ToUserId == conversation.User1Id))
                    .Where(m => !m.IsDeleted)
                    .OrderByDescending(m => m.Timestamp)
                    .FirstOrDefaultAsync();

                conversationDtos.Add(new ConversationDTO(
                    conversation.Id.ToString(),
                    otherUserId,
                    otherUser?.Username ?? "Unknown",
                    lastMessage?.Message ?? "",
                    conversation.LastMessageTime,
                    await _context.ChatMessages
                        .CountAsync(m => m.ToUserId == userId && m.FromUserId == otherUserId && !m.IsRead)
                ));
            }

            return Ok(conversationDtos);
        }

        [HttpGet("messages/with-owner/{currentUserId}/{ownerUserId}")]
        public async Task<ActionResult<List<ChatMessage>>> GetMessagesWithOwner(string currentUserId, string ownerUserId)
        {
            if (string.IsNullOrWhiteSpace(currentUserId) || string.IsNullOrWhiteSpace(ownerUserId))
                return BadRequest("Both currentUserId and ownerUserId are required.");

            var messages = await _context.ChatMessages
                .Where(m => (m.FromUserId == currentUserId && m.ToUserId == ownerUserId) ||
                            (m.FromUserId == ownerUserId && m.ToUserId == currentUserId))
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPost("upload-voice")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<VoiceUploadResponseDto>> UploadVoiceMessage(IFormFile voiceFile, [FromForm] string fromUserId, [FromForm] string toUserId)
        {
            try
            {
                if (voiceFile == null || voiceFile.Length == 0)
                    return BadRequest("Голосовой файл не предоставлен");

                if (string.IsNullOrWhiteSpace(fromUserId) || string.IsNullOrWhiteSpace(toUserId))
                    return BadRequest("FromUserId и ToUserId обязательны");

                if (voiceFile.Length > 10 * 1024 * 1024)
                    return BadRequest("Размер файла не должен превышать 10 МБ");

                var allowedExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a", ".aac" };
                var fileExtension = Path.GetExtension(voiceFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest("Поддерживаемые форматы: mp3, wav, ogg, m4a, aac");
                
                using var stream = voiceFile.OpenReadStream();
                var fileName = $"voice_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid()}{fileExtension}";
                var fileUrl = await _cloudinaryService.UploadVoiceAsync(stream, fileName);

                if (string.IsNullOrEmpty(fileUrl))
                    return StatusCode(500, "Ошибка загрузки файла в облачное хранилище");

                var timestamp = DateTime.UtcNow;
                var message = $"Голосовое сообщение: {voiceFile.FileName}";

                var chatMessage = new ChatMessage
                {
                    FromUserId = fromUserId,
                    ToUserId = toUserId,
                    Message = message,
                    MessageType = "voice",
                    FileUrl = fileUrl,
                    Timestamp = timestamp,
                    IsRead = false
                };

                _context.ChatMessages.Add(chatMessage);
                await UpdateOrCreateConversation(fromUserId, toUserId);
                await _context.SaveChangesAsync();

                var id = chatMessage.Id;
                var ts = chatMessage.Timestamp;

                await _hub.Clients.Group($"User_{toUserId}").SendAsync("ReceiveVoiceMessage", fromUserId, fileUrl, message, ts);
                await _hub.Clients.Group($"User_{toUserId}").SendAsync("ReceiveVoiceMessageV2", fromUserId, id, fileUrl, message, ts);

                await _hub.Clients.Group($"User_{fromUserId}").SendAsync("VoiceMessageSent", toUserId, fileUrl, message, ts);
                await _hub.Clients.Group($"User_{fromUserId}").SendAsync("VoiceMessageSentV2", toUserId, id, fileUrl, message, ts);

                return Ok(new VoiceUploadResponseDto
                {
                    Success = true,
                    FileUrl = fileUrl,
                    FileName = voiceFile.FileName,
                    Message = "Голосовое сообщение успешно отправлено"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка сервера: {ex.Message}");
            }
        }

        [HttpGet("test-image")]
        public IActionResult TestImageEndpoint()
        {
            return Ok("Image endpoint is working");
        }

        [HttpPost("upload-image")]
        [Consumes("multipart/form-data")]
        [Authorize]
        public async Task<ActionResult<VoiceUploadResponseDto>> UploadImageMessage(IFormFile imageFile, [FromForm] string fromUserId, [FromForm] string toUserId)
        {
            try
            {
                if (imageFile == null || imageFile.Length == 0)
                    return BadRequest("Файл изображения не выбран");

                if (imageFile.Length > 10 * 1024 * 1024)
                    return BadRequest("Размер файла не должен превышать 10MB");

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest("Поддерживаемые форматы: jpg, jpeg, png, gif, bmp, webp");
        
                string fileUrl;
                using (var stream = imageFile.OpenReadStream())
                {
                    fileUrl = await _cloudinaryService.UploadAsync(stream, imageFile.FileName);
                }

                if (string.IsNullOrEmpty(fileUrl))
                {
                   return StatusCode(500, "Ошибка загрузки изображения в облако");
                }

                var timestamp = DateTime.UtcNow;
                var message = $"Изображение: {imageFile.FileName}";

                var chatMessage = new ChatMessage
                {
                    FromUserId = fromUserId,
                    ToUserId = toUserId,
                    Message = message,
                    MessageType = "image",
                    FileUrl = fileUrl,
                    Timestamp = timestamp,
                    IsRead = false
                };

                _context.ChatMessages.Add(chatMessage);
                await UpdateOrCreateConversation(fromUserId, toUserId);
                await _context.SaveChangesAsync();
        
                var id = chatMessage.Id;
                var ts = chatMessage.Timestamp;

                await _hub.Clients.Group($"User_{toUserId}").SendAsync("ReceiveImageMessage", fromUserId, fileUrl, message, ts);
                await _hub.Clients.Group($"User_{toUserId}").SendAsync("ReceiveImageMessageV2", fromUserId, id, fileUrl, message, ts);

                await _hub.Clients.Group($"User_{fromUserId}").SendAsync("ImageMessageSent", toUserId, fileUrl, message, ts);
                await _hub.Clients.Group($"User_{fromUserId}").SendAsync("ImageMessageSentV2", toUserId, id, fileUrl, message, ts);


                return Ok(new VoiceUploadResponseDto
                {
                    Success = true,
                    FileUrl = fileUrl,
                    FileName = imageFile.FileName,
                    Message = "Изображение успешно отправлено"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Ошибка сервера при загрузке изображения", 
                    details = ex.Message,
                    success = false 
                });
            }
        }

        private async Task UpdateOrCreateConversation(string user1Id, string user2Id)
        {
            var conversation = await _context.ChatConversations
                .FirstOrDefaultAsync(c => c.User1Id == user1Id && c.User2Id == user2Id);

            if (conversation == null)
            {
                conversation = new ChatConversation
                {
                    User1Id = user1Id,
                    User2Id = user2Id,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageTime = DateTime.UtcNow,
                    IsActive = true
                };
                _context.ChatConversations.Add(conversation);
            }
            else
            {
                conversation.LastMessageTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Поиск пользователей по никнейму для начала чата
        /// </summary>
        /// <param name="query">Поисковый запрос (никнейм, имя или фамилия)</param>
        /// <param name="limit">Максимальное количество результатов (по умолчанию 10)</param>
        /// <returns>Список найденных пользователей</returns>
        [HttpGet("search-users")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserSearchResultDto>>> SearchUsers(
            [FromQuery] string query, 
            [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Query parameter is required");
            }

            if (query.Length < 2)
            {
                return BadRequest("Query must be at least 2 characters long");
            }

            if (limit <= 0 || limit > 50)
            {
                limit = 10;
            }

            try
            {
                var users = await _userService.SearchUsersByUsernameAsync(query, limit);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить информацию о пользователе по ID для чата
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>Информация о пользователе</returns>
        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<ActionResult<UserSearchResultDto>> GetUserForChat(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("UserId is required");
            }

            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Начать новый чат с пользователем
        /// </summary>
        /// <param name="dto">Данные для начала чата</param>
        /// <returns>Результат создания чата</returns>
        [HttpPost("start-chat")]
        [Authorize]
        public async Task<ActionResult<ConversationDTO>> StartChat([FromBody] StartChatDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FromUserId) || string.IsNullOrWhiteSpace(dto.ToUserId))
            {
                return BadRequest("FromUserId and ToUserId are required");
            }

            if (dto.FromUserId == dto.ToUserId)
            {
                return BadRequest("Cannot start chat with yourself");
            }

            try
            {
                var fromUserExists = await _context.Users.AnyAsync(u => u.Id == dto.FromUserId);
                var toUserExists = await _context.Users.AnyAsync(u => u.Id == dto.ToUserId);

                if (!fromUserExists || !toUserExists)
                {
                    return NotFound("One or both users not found");
                }

                await UpdateOrCreateConversation(dto.FromUserId, dto.ToUserId);
                await _context.SaveChangesAsync();

                var otherUser = await _context.Users.FindAsync(dto.ToUserId);
                
                var conversationDto = new ConversationDTO(
                    Guid.NewGuid().ToString(),
                    dto.ToUserId,
                    otherUser?.Username ?? "Unknown",
                    dto.InitialMessage ?? "",
                    DateTime.UtcNow,
                    0
                );

                if (!string.IsNullOrWhiteSpace(dto.InitialMessage))
                {
                    var message = new ChatMessage
                    {
                        FromUserId = dto.FromUserId,
                        ToUserId = dto.ToUserId,
                        Message = dto.InitialMessage,
                        Timestamp = DateTime.UtcNow,
                        IsRead = false
                    };

                    _context.ChatMessages.Add(message);
                    await _context.SaveChangesAsync();
                }

                return Ok(conversationDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
