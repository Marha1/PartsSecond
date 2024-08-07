namespace Application.Dtos.ChatDto
{
    public class RunResponse
    {
        public string Id { get; set; }
        public string Object { get; set; }
        public int CreatedAt { get; set; }
        public string AssistantId { get; set; }
        public string ThreadId { get; set; }
        public string Status { get; set; }
        public int? StartedAt { get; set; }
        public int? ExpiresAt { get; set; }
        public int? CancelledAt { get; set; }
        public int? FailedAt { get; set; }
        public int? CompletedAt { get; set; }
        public string RequiredAction { get; set; }
        public string LastError { get; set; }
        public string Model { get; set; }
        public string Instructions { get; set; }
        public List<Choice> Choices { get; set; }         // <-- Ensure Choices is included
        public List<Tool> Tools { get; set; }
        public List<string> FileIds { get; set; }
        public Metadata Metadata { get; set; }
        public double? Temperature { get; set; }
        public double? TopP { get; set; }
        public object TruncationStrategy { get; set; }
        public object IncompleteDetails { get; set; }
        public object Usage { get; set; }
        public string ResponseFormat { get; set; }
        public string ToolChoice { get; set; }
        public bool? ParallelToolCalls { get; set; }
    }
}