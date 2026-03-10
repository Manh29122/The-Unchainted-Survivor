using UnityEngine;

public abstract class ItemEffectHandle
{
    protected ItemEffectHandle(ItemEffectBase effectDefinition, EffectContext context, UnchaintedItemData itemData, int initialStackCount)
    {
        EffectDefinition = effectDefinition;
        Context = context;
        ItemData = itemData;
        StackCount = Mathf.Max(0, initialStackCount);
    }

    public ItemEffectBase EffectDefinition { get; }
    public EffectContext Context { get; }
    public UnchaintedItemData ItemData { get; }
    public int StackCount { get; private set; }
    public bool IsApplied { get; private set; }
    public bool IsRemoved { get; private set; }

    public void Apply()
    {
        if (IsApplied || IsRemoved)
        {
            return;
        }

        IsApplied = true;
        OnApplied();
    }

    public void UpdateStackCount(int newStackCount)
    {
        if (IsRemoved)
        {
            return;
        }

        int clampedStackCount = Mathf.Max(0, newStackCount);
        int previousStackCount = StackCount;
        if (previousStackCount == clampedStackCount)
        {
            return;
        }

        StackCount = clampedStackCount;

        if (IsApplied)
        {
            OnStackChanged(previousStackCount, clampedStackCount);
        }
    }

    public void Remove()
    {
        if (IsRemoved)
        {
            return;
        }

        IsRemoved = true;

        if (IsApplied)
        {
            OnRemoved();
        }
    }

    protected abstract void OnApplied();
    protected abstract void OnStackChanged(int previousStackCount, int newStackCount);
    protected abstract void OnRemoved();
}

public sealed class DelegatingItemEffectHandle : ItemEffectHandle
{
    public DelegatingItemEffectHandle(ItemEffectBase effectDefinition, EffectContext context, UnchaintedItemData itemData, int initialStackCount)
        : base(effectDefinition, context, itemData, initialStackCount)
    {
    }

    protected override void OnApplied()
    {
        EffectDefinition.OnEffectApplied(Context, ItemData, StackCount);
    }

    protected override void OnStackChanged(int previousStackCount, int newStackCount)
    {
        EffectDefinition.OnEffectStackChanged(Context, ItemData, previousStackCount, newStackCount);
    }

    protected override void OnRemoved()
    {
        EffectDefinition.OnEffectRemoved(Context, ItemData, StackCount);
    }
}
