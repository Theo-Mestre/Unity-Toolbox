public interface IInputConsumer
{
    public InputContext Context { get; }
    public void BindInputs(PlayerInputConfig input);
}
