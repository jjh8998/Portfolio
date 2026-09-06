public interface IEscapeClosable
{
    bool IsOpen { get; }
    bool CloseByEscape();
}
