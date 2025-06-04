using Robust.Shared.Serialization;

namespace Content.Shared._Mono.MonoCoins;

/// <summary>
/// Message sent by client to request their MonoCoins balance.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestMonoCoinsBalanceMessage : EntityEventArgs
{
}

/// <summary>
/// Message sent by server in response to MonoCoins balance request.
/// </summary>
[Serializable, NetSerializable]
public sealed class MonoCoinsBalanceResponseMessage : EntityEventArgs
{
    public int Balance { get; set; }
}
