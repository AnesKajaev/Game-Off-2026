public interface ITaskCondition
{
    bool CanStart { get; }
    void OnBlocked();
}