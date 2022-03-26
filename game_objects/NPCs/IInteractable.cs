
/// <summary>
/// An interactable npc. The player can press "E" on them.
/// </summary>
public interface IInteractable
{
    bool Interact(Player player);
    string GetInteractMessage();
}