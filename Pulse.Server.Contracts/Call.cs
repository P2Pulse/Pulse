namespace Pulse.Server.Contracts;

public class Call
{
    public string Id { get; set; } = default!;
    public string Caller { get; set; } = default!;
    public string Callee { get; set; } = default!;
    public DateTime CallTime { get; set; }
    
    public DateTime? AnswerTime { get; set; }
    public DateTime? EndTime { get; set; }
}