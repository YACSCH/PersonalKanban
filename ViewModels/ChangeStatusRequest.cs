namespace PersonalKanban.ViewModels
{
    public class ChangeStatusRequest
    {
        public int TaskId { get; set; }
        public int StatusId { get; set; }
    }
}
