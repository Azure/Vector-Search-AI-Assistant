using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorSearchAiAssistant.SemanticKernel.Chat
{
    public class ChatBuilder
    {
        readonly IKernel _kernel;
        string _prompt = string.Empty;
        List<object> _memories = new List<object>();
        List<(AuthorRole AuthorRole, string Content)> _messages = new List<(AuthorRole AuthorRole, string Content)>();

        public ChatBuilder(
            IKernel kernel) 
        {
            _kernel = kernel;
        }

        public ChatBuilder WithSystemPrompt(string prompt)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(prompt, nameof(prompt));
            _prompt = prompt;
            return this;
        }

        public ChatBuilder WithMemories(List<object> memories)
        {
            ArgumentNullException.ThrowIfNull(memories, nameof(memories));
            _memories = memories;
            return this;
        }

        public ChatBuilder WithMessageHistory(List<(AuthorRole AuthorRole, string Content)> messages) 
        {
            ArgumentNullException.ThrowIfNull(messages, nameof(messages));
            _messages = messages;
            return this;
        }

        public ChatHistory Build()
        {
            // TODO: Add logic to filter memories and message history to avoid exceeding the max prompt size

            var result = _kernel.GetService<IChatCompletion>()
                .CreateNewChat();

            var systemMessage = string.IsNullOrWhiteSpace(_prompt)
                ? string.Empty
                : _prompt;

            if (_memories.Count > 0)
                systemMessage = $"{systemMessage}{Environment.NewLine}{Environment.NewLine}{JsonConvert.SerializeObject(_memories)}";

            if (!string.IsNullOrWhiteSpace(systemMessage)) 
                result.AddSystemMessage(systemMessage);

            foreach (var message in _messages)
                result.AddMessage(message.AuthorRole, message.Content);

            return result;
        }
    }
}
